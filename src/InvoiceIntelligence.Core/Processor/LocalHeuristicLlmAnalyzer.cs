namespace InvoiceIntelligence.Core.Processor;

public sealed class LocalHeuristicLlmAnalyzer : ILocalLlmAnalyzer
{
    public InvoiceDecision Analyze(InvoiceAnalysisRecord record, IReadOnlyList<RetrievedReferenceCase> referenceCases)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(referenceCases);

        var itemName = record.ItemName.Trim();
        var hasHighConfidenceReference = referenceCases.Any(item => item.SimilarityScore >= 0.80m);
        var confidenceBoost = hasHighConfidenceReference ? 0.08m : 0m;

        if (ContainsAny(itemName, "禮品", "餐敘", "娛樂", "招待"))
        {
            return BuildDecision(
                deductibleStatus: "不可抵稅",
                deductionReason: "本地 LLM 判定為交際或招待性質支出，不得作為進項抵扣。",
                accountingCategory: "交際費",
                confidence: 0.84m + confidenceBoost);
        }

        if (ContainsAny(itemName, "運費", "文具", "軟體", "維護", "租金"))
        {
            return BuildDecision(
                deductibleStatus: "可抵稅",
                deductionReason: "本地 LLM 判定與營業相關，可列為進項抵扣。",
                accountingCategory: "營業費用",
                confidence: 0.82m + confidenceBoost);
        }

        if (record.TotalAmount >= 50000m)
        {
            return BuildDecision(
                deductibleStatus: "需人工覆核",
                deductionReason: "本地 LLM 判定金額偏高且缺少明確規則，建議人工複核。",
                accountingCategory: "待判定",
                confidence: 0.66m + confidenceBoost);
        }

        return BuildDecision(
            deductibleStatus: "可抵稅",
            deductionReason: "本地 LLM 依一般營業支出語意判定可抵稅。",
            accountingCategory: "營業費用",
            confidence: 0.74m + confidenceBoost);
    }

    private static InvoiceDecision BuildDecision(
        string deductibleStatus,
        string deductionReason,
        string accountingCategory,
        decimal confidence)
    {
        var normalizedConfidence = Math.Clamp(confidence, 0m, 1m);

        return new InvoiceDecision(
            DeductibleStatus: deductibleStatus,
            DeductionReason: deductionReason,
            AccountingCategory: accountingCategory,
            ConfidenceScore: decimal.Round(normalizedConfidence, 2, MidpointRounding.AwayFromZero));
    }

    private static bool ContainsAny(string text, params string[] candidates)
    {
        return candidates.Any(candidate =>
            text.Contains(candidate, StringComparison.OrdinalIgnoreCase));
    }
}
