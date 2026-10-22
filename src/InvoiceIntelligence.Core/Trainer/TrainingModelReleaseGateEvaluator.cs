namespace InvoiceIntelligence.Core.Trainer;

public sealed class TrainingModelReleaseGateEvaluator
{
    public TrainingModelReleaseGateDecision Evaluate(TrainingModelReleaseGateInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Metrics);
        ArgumentNullException.ThrowIfNull(input.Thresholds);
        ArgumentNullException.ThrowIfNull(input.Baseline);

        var blockedRules = EvaluateBlockRules(input);
        if (blockedRules.Count > 0)
        {
            return new TrainingModelReleaseGateDecision(
                GateResult: TrainingModelReleaseGateResult.Block,
                BlockedRules: blockedRules,
                BreachedRules: [],
                WarningRules: [],
                SummaryReason: "Required release-gate metadata or sample-quality checks failed.",
                EvaluatedAtUtc: DateTimeOffset.UtcNow,
                RollbackMonitoringRequired: false);
        }

        var failRules = EvaluateFailRules(input);
        if (failRules.Count > 0)
        {
            return new TrainingModelReleaseGateDecision(
                GateResult: TrainingModelReleaseGateResult.Fail,
                BlockedRules: [],
                BreachedRules: failRules,
                WarningRules: [],
                SummaryReason: "One or more fail thresholds were breached.",
                EvaluatedAtUtc: DateTimeOffset.UtcNow,
                RollbackMonitoringRequired: true);
        }

        var warningRules = EvaluateWarningRules(input);
        if (warningRules.Count > 0)
        {
            return new TrainingModelReleaseGateDecision(
                GateResult: TrainingModelReleaseGateResult.Warn,
                BlockedRules: [],
                BreachedRules: [],
                WarningRules: warningRules,
                SummaryReason: "Warning thresholds were crossed. Release can proceed with explicit approval.",
                EvaluatedAtUtc: DateTimeOffset.UtcNow,
                RollbackMonitoringRequired: true);
        }

        return new TrainingModelReleaseGateDecision(
            GateResult: TrainingModelReleaseGateResult.Pass,
            BlockedRules: [],
            BreachedRules: [],
            WarningRules: [],
            SummaryReason: "All release-gate thresholds passed.",
            EvaluatedAtUtc: DateTimeOffset.UtcNow,
            RollbackMonitoringRequired: true);
    }

    public TrainingModelRollbackDecision EvaluateRollback(
        TrainingModelProductionSnapshot current,
        TrainingModelReleaseBaseline baseline,
        TrainingModelRollbackThresholdProfile thresholds)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(thresholds);

        var categories = new List<string>();
        var reasons = new List<string>();

        var deductibleDrop = baseline.DeductibleStatusAccuracy - current.DeductibleStatusAccuracy;
        if (deductibleDrop > thresholds.MaximumDeductibleStatusAccuracyDrop)
        {
            AddTrigger(
                categories,
                reasons,
                category: "quality-regression",
                reason: $"Deductible status accuracy dropped by {deductibleDrop:P2}.");
        }

        var categoryF1Drop = baseline.AccountingCategoryMacroF1 - current.AccountingCategoryMacroF1;
        if (categoryF1Drop > thresholds.MaximumAccountingCategoryMacroF1Drop)
        {
            AddTrigger(
                categories,
                reasons,
                category: "quality-regression",
                reason: $"Accounting category F1 dropped by {categoryF1Drop:P2}.");
        }

        var reasonScoreDrop = baseline.DeductionReasonQualityScore - current.DeductionReasonQualityScore;
        if (reasonScoreDrop > thresholds.MaximumDeductionReasonQualityScoreDrop)
        {
            AddTrigger(
                categories,
                reasons,
                category: "quality-regression",
                reason: $"Deduction reason quality score dropped by {reasonScoreDrop:P2}.");
        }

        if (current.NonDeductibleFalsePositiveRate > thresholds.MaximumNonDeductibleFalsePositiveRate)
        {
            AddTrigger(
                categories,
                reasons,
                category: "safety",
                reason: $"Non-deductible false positive rate is {current.NonDeductibleFalsePositiveRate:P2}.");
        }

        if (current.InferenceLatencyP95Ms > thresholds.MaximumInferenceLatencyP95Ms)
        {
            AddTrigger(
                categories,
                reasons,
                category: "stability",
                reason: $"Inference p95 latency reached {current.InferenceLatencyP95Ms} ms.");
        }

        if (current.ManualReviewRate > thresholds.MaximumManualReviewRate)
        {
            AddTrigger(
                categories,
                reasons,
                category: "stability",
                reason: $"Manual review rate increased to {current.ManualReviewRate:P2}.");
        }

        var shouldRollback = reasons.Count > 0;
        var recommendation = shouldRollback
            ? "Switch runtime to the previous approved model (or baseline pipeline) and open rollback incident review."
            : "Continue monitoring; rollback conditions are not met.";

        return new TrainingModelRollbackDecision(
            ShouldRollback: shouldRollback,
            TriggerCategories: categories,
            TriggerReasons: reasons,
            RecommendedAction: recommendation,
            EvaluatedAtUtc: DateTimeOffset.UtcNow);
    }

    private static IReadOnlyList<string> EvaluateBlockRules(TrainingModelReleaseGateInput input)
    {
        var rules = new List<string>();
        var metrics = input.Metrics;
        var thresholds = input.Thresholds;

        if (string.IsNullOrWhiteSpace(metrics.ModelVersion))
        {
            rules.Add("BLOCK_MODEL_VERSION_REQUIRED");
        }

        if (string.IsNullOrWhiteSpace(metrics.TrainingDatasetVersion))
        {
            rules.Add("BLOCK_DATASET_VERSION_REQUIRED");
        }

        if (string.IsNullOrWhiteSpace(metrics.GovernancePolicyVersion))
        {
            rules.Add("BLOCK_GOVERNANCE_POLICY_VERSION_REQUIRED");
        }

        if (string.IsNullOrWhiteSpace(metrics.ReleaseCandidateId))
        {
            rules.Add("BLOCK_RELEASE_CANDIDATE_ID_REQUIRED");
        }

        if (string.IsNullOrWhiteSpace(input.EvaluatorVersion))
        {
            rules.Add("BLOCK_EVALUATOR_VERSION_REQUIRED");
        }

        if (metrics.EvaluationWindowStart > metrics.EvaluationWindowEnd)
        {
            rules.Add("BLOCK_INVALID_EVALUATION_WINDOW");
        }

        if (metrics.TotalSamples <= 0)
        {
            rules.Add("BLOCK_TOTAL_SAMPLES_INVALID");
        }

        if (metrics.CoveredSamples < 0 || metrics.CoveredSamples > metrics.TotalSamples)
        {
            rules.Add("BLOCK_COVERED_SAMPLES_INVALID");
        }

        if (metrics.TotalSamples < thresholds.MinimumSamples)
        {
            rules.Add("BLOCK_MINIMUM_SAMPLE_SIZE_NOT_MET");
        }

        if (metrics.CoverageRate < thresholds.MinimumCoverageRate)
        {
            rules.Add("BLOCK_MINIMUM_COVERAGE_RATE_NOT_MET");
        }

        ValidateRate(rules, "BLOCK_RATE_RANGE_INVALID_DEDUCTIBLE_STATUS_ACCURACY", metrics.DeductibleStatusAccuracy);
        ValidateRate(rules, "BLOCK_RATE_RANGE_INVALID_ACCOUNTING_CATEGORY_F1", metrics.AccountingCategoryMacroF1);
        ValidateRate(rules, "BLOCK_RATE_RANGE_INVALID_REASON_QUALITY_SCORE", metrics.DeductionReasonQualityScore);
        ValidateRate(rules, "BLOCK_RATE_RANGE_INVALID_CONFIDENCE_CALIBRATION_ERROR", metrics.ConfidenceCalibrationError);
        ValidateRate(rules, "BLOCK_RATE_RANGE_INVALID_UNRESOLVED_RATE", metrics.UnresolvedRate);
        ValidateRate(rules, "BLOCK_RATE_RANGE_INVALID_MANUAL_REVIEW_RATE", metrics.ManualReviewRate);
        ValidateRate(rules, "BLOCK_RATE_RANGE_INVALID_NON_DEDUCTIBLE_FALSE_POSITIVE_RATE", metrics.NonDeductibleFalsePositiveRate);

        if (metrics.EvaluationDurationMs <= 0)
        {
            rules.Add("BLOCK_EVALUATION_DURATION_INVALID");
        }

        if (metrics.InferenceLatencyP95Ms <= 0)
        {
            rules.Add("BLOCK_INFERENCE_LATENCY_P95_INVALID");
        }

        return rules;
    }

    private static IReadOnlyList<string> EvaluateFailRules(TrainingModelReleaseGateInput input)
    {
        var rules = new List<string>();
        var metrics = input.Metrics;
        var thresholds = input.Thresholds;
        var baseline = input.Baseline;

        if (metrics.DeductibleStatusAccuracy < thresholds.MinimumDeductibleStatusAccuracyFail)
        {
            rules.Add("FAIL_DEDUCTIBLE_STATUS_ACCURACY");
        }

        if (metrics.AccountingCategoryMacroF1 < thresholds.MinimumAccountingCategoryMacroF1Fail)
        {
            rules.Add("FAIL_ACCOUNTING_CATEGORY_MACRO_F1");
        }

        if (metrics.DeductionReasonQualityScore < thresholds.MinimumDeductionReasonQualityScoreFail)
        {
            rules.Add("FAIL_DEDUCTION_REASON_QUALITY_SCORE");
        }

        if (metrics.ConfidenceCalibrationError > thresholds.MaximumConfidenceCalibrationErrorFail)
        {
            rules.Add("FAIL_CONFIDENCE_CALIBRATION_ERROR");
        }

        if (metrics.UnresolvedRate > thresholds.MaximumUnresolvedRateFail)
        {
            rules.Add("FAIL_UNRESOLVED_RATE");
        }

        if (metrics.ManualReviewRate > thresholds.MaximumManualReviewRateFail)
        {
            rules.Add("FAIL_MANUAL_REVIEW_RATE");
        }

        if (metrics.DriftDeltaVsBaseline > thresholds.MaximumDriftDeltaFail)
        {
            rules.Add("FAIL_DRIFT_DELTA_VS_BASELINE");
        }

        if (metrics.EvaluationDurationMs > thresholds.MaximumEvaluationDurationMsFail)
        {
            rules.Add("FAIL_EVALUATION_DURATION_MS");
        }

        if (metrics.InferenceLatencyP95Ms > thresholds.MaximumInferenceLatencyP95MsFail)
        {
            rules.Add("FAIL_INFERENCE_LATENCY_P95_MS");
        }

        if (baseline.DeductibleStatusAccuracy - metrics.DeductibleStatusAccuracy >
            thresholds.MaximumDeductibleStatusAccuracyDropFail)
        {
            rules.Add("FAIL_DEDUCTIBLE_STATUS_ACCURACY_REGRESSION");
        }

        if (baseline.AccountingCategoryMacroF1 - metrics.AccountingCategoryMacroF1 >
            thresholds.MaximumAccountingCategoryMacroF1DropFail)
        {
            rules.Add("FAIL_ACCOUNTING_CATEGORY_F1_REGRESSION");
        }

        if (baseline.DeductionReasonQualityScore - metrics.DeductionReasonQualityScore >
            thresholds.MaximumDeductionReasonQualityScoreDropFail)
        {
            rules.Add("FAIL_DEDUCTION_REASON_QUALITY_REGRESSION");
        }

        var latencyIncrease = (decimal)metrics.InferenceLatencyP95Ms / Math.Max(1, baseline.InferenceLatencyP95Ms) - 1m;
        if (latencyIncrease > thresholds.MaximumInferenceLatencyIncreaseRatioFail)
        {
            rules.Add("FAIL_INFERENCE_LATENCY_REGRESSION");
        }

        return rules;
    }

    private static IReadOnlyList<string> EvaluateWarningRules(TrainingModelReleaseGateInput input)
    {
        var rules = new List<string>();
        var metrics = input.Metrics;
        var thresholds = input.Thresholds;
        var baseline = input.Baseline;

        if (metrics.DeductibleStatusAccuracy < thresholds.MinimumDeductibleStatusAccuracyWarn)
        {
            rules.Add("WARN_DEDUCTIBLE_STATUS_ACCURACY");
        }

        if (metrics.AccountingCategoryMacroF1 < thresholds.MinimumAccountingCategoryMacroF1Warn)
        {
            rules.Add("WARN_ACCOUNTING_CATEGORY_MACRO_F1");
        }

        if (metrics.DeductionReasonQualityScore < thresholds.MinimumDeductionReasonQualityScoreWarn)
        {
            rules.Add("WARN_DEDUCTION_REASON_QUALITY_SCORE");
        }

        if (metrics.ConfidenceCalibrationError > thresholds.MaximumConfidenceCalibrationErrorWarn)
        {
            rules.Add("WARN_CONFIDENCE_CALIBRATION_ERROR");
        }

        if (metrics.UnresolvedRate > thresholds.MaximumUnresolvedRateWarn)
        {
            rules.Add("WARN_UNRESOLVED_RATE");
        }

        if (metrics.ManualReviewRate > thresholds.MaximumManualReviewRateWarn)
        {
            rules.Add("WARN_MANUAL_REVIEW_RATE");
        }

        if (metrics.DriftDeltaVsBaseline > thresholds.MaximumDriftDeltaWarn)
        {
            rules.Add("WARN_DRIFT_DELTA_VS_BASELINE");
        }

        if (metrics.EvaluationDurationMs > thresholds.MaximumEvaluationDurationMsWarn)
        {
            rules.Add("WARN_EVALUATION_DURATION_MS");
        }

        if (metrics.InferenceLatencyP95Ms > thresholds.MaximumInferenceLatencyP95MsWarn)
        {
            rules.Add("WARN_INFERENCE_LATENCY_P95_MS");
        }

        if (baseline.DeductibleStatusAccuracy - metrics.DeductibleStatusAccuracy >
            thresholds.MaximumDeductibleStatusAccuracyDropWarn)
        {
            rules.Add("WARN_DEDUCTIBLE_STATUS_ACCURACY_REGRESSION");
        }

        if (baseline.AccountingCategoryMacroF1 - metrics.AccountingCategoryMacroF1 >
            thresholds.MaximumAccountingCategoryMacroF1DropWarn)
        {
            rules.Add("WARN_ACCOUNTING_CATEGORY_F1_REGRESSION");
        }

        if (baseline.DeductionReasonQualityScore - metrics.DeductionReasonQualityScore >
            thresholds.MaximumDeductionReasonQualityScoreDropWarn)
        {
            rules.Add("WARN_DEDUCTION_REASON_QUALITY_REGRESSION");
        }

        var latencyIncrease = (decimal)metrics.InferenceLatencyP95Ms / Math.Max(1, baseline.InferenceLatencyP95Ms) - 1m;
        if (latencyIncrease > thresholds.MaximumInferenceLatencyIncreaseRatioWarn)
        {
            rules.Add("WARN_INFERENCE_LATENCY_REGRESSION");
        }

        return rules;
    }

    private static void ValidateRate(ICollection<string> rules, string ruleCode, decimal value)
    {
        if (value < 0m || value > 1m)
        {
            rules.Add(ruleCode);
        }
    }

    private static void AddTrigger(
        ICollection<string> categories,
        ICollection<string> reasons,
        string category,
        string reason)
    {
        if (!categories.Contains(category, StringComparer.Ordinal))
        {
            categories.Add(category);
        }

        reasons.Add(reason);
    }
}

public sealed record TrainingModelReleaseGateInput(
    TrainingModelReleaseMetrics Metrics,
    TrainingModelReleaseThresholdProfile Thresholds,
    TrainingModelReleaseBaseline Baseline,
    string EvaluatorVersion);

public sealed record TrainingModelReleaseMetrics(
    string ModelVersion,
    string TrainingDatasetVersion,
    string GovernancePolicyVersion,
    string ReleaseCandidateId,
    DateOnly EvaluationWindowStart,
    DateOnly EvaluationWindowEnd,
    int TotalSamples,
    int CoveredSamples,
    decimal DeductibleStatusAccuracy,
    decimal AccountingCategoryMacroF1,
    decimal DeductionReasonQualityScore,
    decimal ConfidenceCalibrationError,
    decimal DriftDeltaVsBaseline,
    decimal UnresolvedRate,
    decimal ManualReviewRate,
    int EvaluationDurationMs,
    int InferenceLatencyP95Ms,
    decimal NonDeductibleFalsePositiveRate)
{
    public decimal CoverageRate => TotalSamples <= 0
        ? 0m
        : Math.Round((decimal)CoveredSamples / TotalSamples, 4);
}

public sealed record TrainingModelReleaseBaseline(
    decimal DeductibleStatusAccuracy,
    decimal AccountingCategoryMacroF1,
    decimal DeductionReasonQualityScore,
    int InferenceLatencyP95Ms);

public sealed record TrainingModelReleaseThresholdProfile(
    int MinimumSamples,
    decimal MinimumCoverageRate,
    decimal MinimumDeductibleStatusAccuracyWarn,
    decimal MinimumDeductibleStatusAccuracyFail,
    decimal MinimumAccountingCategoryMacroF1Warn,
    decimal MinimumAccountingCategoryMacroF1Fail,
    decimal MinimumDeductionReasonQualityScoreWarn,
    decimal MinimumDeductionReasonQualityScoreFail,
    decimal MaximumConfidenceCalibrationErrorWarn,
    decimal MaximumConfidenceCalibrationErrorFail,
    decimal MaximumUnresolvedRateWarn,
    decimal MaximumUnresolvedRateFail,
    decimal MaximumManualReviewRateWarn,
    decimal MaximumManualReviewRateFail,
    decimal MaximumDriftDeltaWarn,
    decimal MaximumDriftDeltaFail,
    int MaximumEvaluationDurationMsWarn,
    int MaximumEvaluationDurationMsFail,
    int MaximumInferenceLatencyP95MsWarn,
    int MaximumInferenceLatencyP95MsFail,
    decimal MaximumDeductibleStatusAccuracyDropWarn,
    decimal MaximumDeductibleStatusAccuracyDropFail,
    decimal MaximumAccountingCategoryMacroF1DropWarn,
    decimal MaximumAccountingCategoryMacroF1DropFail,
    decimal MaximumDeductionReasonQualityScoreDropWarn,
    decimal MaximumDeductionReasonQualityScoreDropFail,
    decimal MaximumInferenceLatencyIncreaseRatioWarn,
    decimal MaximumInferenceLatencyIncreaseRatioFail)
{
    public static TrainingModelReleaseThresholdProfile Default => new(
        MinimumSamples: 2000,
        MinimumCoverageRate: 0.90m,
        MinimumDeductibleStatusAccuracyWarn: 0.96m,
        MinimumDeductibleStatusAccuracyFail: 0.94m,
        MinimumAccountingCategoryMacroF1Warn: 0.91m,
        MinimumAccountingCategoryMacroF1Fail: 0.88m,
        MinimumDeductionReasonQualityScoreWarn: 0.88m,
        MinimumDeductionReasonQualityScoreFail: 0.85m,
        MaximumConfidenceCalibrationErrorWarn: 0.10m,
        MaximumConfidenceCalibrationErrorFail: 0.12m,
        MaximumUnresolvedRateWarn: 0.06m,
        MaximumUnresolvedRateFail: 0.10m,
        MaximumManualReviewRateWarn: 0.14m,
        MaximumManualReviewRateFail: 0.18m,
        MaximumDriftDeltaWarn: 0.08m,
        MaximumDriftDeltaFail: 0.12m,
        MaximumEvaluationDurationMsWarn: 600000,
        MaximumEvaluationDurationMsFail: 900000,
        MaximumInferenceLatencyP95MsWarn: 1200,
        MaximumInferenceLatencyP95MsFail: 1500,
        MaximumDeductibleStatusAccuracyDropWarn: 0.015m,
        MaximumDeductibleStatusAccuracyDropFail: 0.03m,
        MaximumAccountingCategoryMacroF1DropWarn: 0.02m,
        MaximumAccountingCategoryMacroF1DropFail: 0.04m,
        MaximumDeductionReasonQualityScoreDropWarn: 0.02m,
        MaximumDeductionReasonQualityScoreDropFail: 0.05m,
        MaximumInferenceLatencyIncreaseRatioWarn: 0.15m,
        MaximumInferenceLatencyIncreaseRatioFail: 0.30m);
}

public sealed record TrainingModelProductionSnapshot(
    decimal DeductibleStatusAccuracy,
    decimal AccountingCategoryMacroF1,
    decimal DeductionReasonQualityScore,
    decimal NonDeductibleFalsePositiveRate,
    int InferenceLatencyP95Ms,
    decimal ManualReviewRate);

public sealed record TrainingModelRollbackThresholdProfile(
    decimal MaximumDeductibleStatusAccuracyDrop,
    decimal MaximumAccountingCategoryMacroF1Drop,
    decimal MaximumDeductionReasonQualityScoreDrop,
    decimal MaximumNonDeductibleFalsePositiveRate,
    int MaximumInferenceLatencyP95Ms,
    decimal MaximumManualReviewRate)
{
    public static TrainingModelRollbackThresholdProfile Default => new(
        MaximumDeductibleStatusAccuracyDrop: 0.03m,
        MaximumAccountingCategoryMacroF1Drop: 0.04m,
        MaximumDeductionReasonQualityScoreDrop: 0.05m,
        MaximumNonDeductibleFalsePositiveRate: 0.03m,
        MaximumInferenceLatencyP95Ms: 1800,
        MaximumManualReviewRate: 0.20m);
}

public sealed record TrainingModelReleaseGateDecision(
    TrainingModelReleaseGateResult GateResult,
    IReadOnlyList<string> BlockedRules,
    IReadOnlyList<string> BreachedRules,
    IReadOnlyList<string> WarningRules,
    string SummaryReason,
    DateTimeOffset EvaluatedAtUtc,
    bool RollbackMonitoringRequired);

public sealed record TrainingModelRollbackDecision(
    bool ShouldRollback,
    IReadOnlyList<string> TriggerCategories,
    IReadOnlyList<string> TriggerReasons,
    string RecommendedAction,
    DateTimeOffset EvaluatedAtUtc);

public enum TrainingModelReleaseGateResult
{
    Pass = 0,
    Warn = 1,
    Fail = 2,
    Block = 3
}
