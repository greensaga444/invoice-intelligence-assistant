using InvoiceIntelligence.Core.Trainer;

namespace InvoiceIntelligence.Core.Tests.Trainer;

public class TrainingModelReleaseGateEvaluatorTests
{
    [Fact]
    public void Evaluate_WithMissingMetadata_ReturnsBlock()
    {
        var evaluator = new TrainingModelReleaseGateEvaluator();
        var input = CreateInput(metrics: CreateMetrics() with
        {
            ModelVersion = string.Empty
        });

        var result = evaluator.Evaluate(input);

        Assert.Equal(TrainingModelReleaseGateResult.Block, result.GateResult);
        Assert.Contains("BLOCK_MODEL_VERSION_REQUIRED", result.BlockedRules);
        Assert.Empty(result.BreachedRules);
        Assert.Empty(result.WarningRules);
    }

    [Fact]
    public void Evaluate_WithFailThresholdBreach_ReturnsFail()
    {
        var evaluator = new TrainingModelReleaseGateEvaluator();
        var input = CreateInput(metrics: CreateMetrics() with
        {
            DeductibleStatusAccuracy = 0.90m,
            AccountingCategoryMacroF1 = 0.86m,
            DeductionReasonQualityScore = 0.84m
        });

        var result = evaluator.Evaluate(input);

        Assert.Equal(TrainingModelReleaseGateResult.Fail, result.GateResult);
        Assert.Contains("FAIL_DEDUCTIBLE_STATUS_ACCURACY", result.BreachedRules);
        Assert.Contains("FAIL_ACCOUNTING_CATEGORY_MACRO_F1", result.BreachedRules);
        Assert.Contains("FAIL_DEDUCTION_REASON_QUALITY_SCORE", result.BreachedRules);
    }

    [Fact]
    public void Evaluate_WithWarningOnly_ReturnsWarn()
    {
        var evaluator = new TrainingModelReleaseGateEvaluator();
        var input = CreateInput(metrics: CreateMetrics() with
        {
            DeductibleStatusAccuracy = 0.955m,
            AccountingCategoryMacroF1 = 0.905m,
            DeductionReasonQualityScore = 0.875m
        });

        var result = evaluator.Evaluate(input);

        Assert.Equal(TrainingModelReleaseGateResult.Warn, result.GateResult);
        Assert.Contains("WARN_DEDUCTIBLE_STATUS_ACCURACY", result.WarningRules);
        Assert.Contains("WARN_ACCOUNTING_CATEGORY_MACRO_F1", result.WarningRules);
        Assert.Contains("WARN_DEDUCTION_REASON_QUALITY_SCORE", result.WarningRules);
        Assert.Empty(result.BreachedRules);
    }

    [Fact]
    public void Evaluate_WithHealthyMetrics_ReturnsPass()
    {
        var evaluator = new TrainingModelReleaseGateEvaluator();
        var input = CreateInput();

        var result = evaluator.Evaluate(input);

        Assert.Equal(TrainingModelReleaseGateResult.Pass, result.GateResult);
        Assert.Empty(result.BlockedRules);
        Assert.Empty(result.BreachedRules);
        Assert.Empty(result.WarningRules);
    }

    [Fact]
    public void EvaluateRollback_WithQualityAndSafetyRegression_ReturnsRollback()
    {
        var evaluator = new TrainingModelReleaseGateEvaluator();
        var current = new TrainingModelProductionSnapshot(
            DeductibleStatusAccuracy: 0.90m,
            AccountingCategoryMacroF1: 0.86m,
            DeductionReasonQualityScore: 0.80m,
            NonDeductibleFalsePositiveRate: 0.05m,
            InferenceLatencyP95Ms: 2000,
            ManualReviewRate: 0.25m);
        var baseline = new TrainingModelReleaseBaseline(
            DeductibleStatusAccuracy: 0.97m,
            AccountingCategoryMacroF1: 0.92m,
            DeductionReasonQualityScore: 0.90m,
            InferenceLatencyP95Ms: 900);

        var result = evaluator.EvaluateRollback(current, baseline, TrainingModelRollbackThresholdProfile.Default);

        Assert.True(result.ShouldRollback);
        Assert.Contains("quality-regression", result.TriggerCategories);
        Assert.Contains("safety", result.TriggerCategories);
        Assert.Contains("stability", result.TriggerCategories);
        Assert.True(result.TriggerReasons.Count >= 3);
    }

    [Fact]
    public void EvaluateRollback_WithHealthyProductionMetrics_ReturnsNoRollback()
    {
        var evaluator = new TrainingModelReleaseGateEvaluator();
        var current = new TrainingModelProductionSnapshot(
            DeductibleStatusAccuracy: 0.965m,
            AccountingCategoryMacroF1: 0.915m,
            DeductionReasonQualityScore: 0.89m,
            NonDeductibleFalsePositiveRate: 0.01m,
            InferenceLatencyP95Ms: 1000,
            ManualReviewRate: 0.08m);
        var baseline = new TrainingModelReleaseBaseline(
            DeductibleStatusAccuracy: 0.97m,
            AccountingCategoryMacroF1: 0.92m,
            DeductionReasonQualityScore: 0.90m,
            InferenceLatencyP95Ms: 900);

        var result = evaluator.EvaluateRollback(current, baseline, TrainingModelRollbackThresholdProfile.Default);

        Assert.False(result.ShouldRollback);
        Assert.Empty(result.TriggerCategories);
        Assert.Empty(result.TriggerReasons);
    }

    private static TrainingModelReleaseGateInput CreateInput(
        TrainingModelReleaseMetrics? metrics = null,
        TrainingModelReleaseThresholdProfile? thresholds = null,
        TrainingModelReleaseBaseline? baseline = null,
        string evaluatorVersion = "v1.0.0")
    {
        return new TrainingModelReleaseGateInput(
            Metrics: metrics ?? CreateMetrics(),
            Thresholds: thresholds ?? TrainingModelReleaseThresholdProfile.Default,
            Baseline: baseline ?? new TrainingModelReleaseBaseline(
                DeductibleStatusAccuracy: 0.97m,
                AccountingCategoryMacroF1: 0.92m,
                DeductionReasonQualityScore: 0.90m,
                InferenceLatencyP95Ms: 900),
            EvaluatorVersion: evaluatorVersion);
    }

    private static TrainingModelReleaseMetrics CreateMetrics()
    {
        return new TrainingModelReleaseMetrics(
            ModelVersion: "model-2026.10.22",
            TrainingDatasetVersion: "dataset-v5",
            GovernancePolicyVersion: "1.0.0",
            ReleaseCandidateId: "rc-20261022-01",
            EvaluationWindowStart: new DateOnly(2026, 10, 1),
            EvaluationWindowEnd: new DateOnly(2026, 10, 21),
            TotalSamples: 5000,
            CoveredSamples: 4900,
            DeductibleStatusAccuracy: 0.97m,
            AccountingCategoryMacroF1: 0.92m,
            DeductionReasonQualityScore: 0.90m,
            ConfidenceCalibrationError: 0.07m,
            DriftDeltaVsBaseline: 0.03m,
            UnresolvedRate: 0.03m,
            ManualReviewRate: 0.08m,
            EvaluationDurationMs: 420000,
            InferenceLatencyP95Ms: 980,
            NonDeductibleFalsePositiveRate: 0.01m);
    }
}
