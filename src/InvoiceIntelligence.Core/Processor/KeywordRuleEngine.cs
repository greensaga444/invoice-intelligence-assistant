namespace InvoiceIntelligence.Core.Processor;

public sealed class KeywordRuleEngine : IRuleEngine
{
    private static readonly RuleDefinition[] Rules =
    [
        new RuleDefinition(
            Keyword: "文具",
            DeductibleStatus: "可抵稅",
            DeductionReason: "規則判定：辦公用品可列為進項抵扣。",
            AccountingCategory: "辦公用品費"),
        new RuleDefinition(
            Keyword: "影印紙",
            DeductibleStatus: "可抵稅",
            DeductionReason: "規則判定：耗材性辦公用品可列為進項抵扣。",
            AccountingCategory: "辦公用品費"),
        new RuleDefinition(
            Keyword: "餐飲",
            DeductibleStatus: "不可抵稅",
            DeductionReason: "規則判定：餐飲交際支出不列抵。",
            AccountingCategory: "交際費"),
        new RuleDefinition(
            Keyword: "招待",
            DeductibleStatus: "不可抵稅",
            DeductionReason: "規則判定：招待相關支出不列抵。",
            AccountingCategory: "交際費")
    ];

    public InvoiceDecision? TryResolve(InvoiceAnalysisRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        foreach (var rule in Rules)
        {
            if (!record.ItemName.Contains(rule.Keyword, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return new InvoiceDecision(
                DeductibleStatus: rule.DeductibleStatus,
                DeductionReason: rule.DeductionReason,
                AccountingCategory: rule.AccountingCategory,
                ConfidenceScore: 0.99m);
        }

        return null;
    }

    private sealed record RuleDefinition(
        string Keyword,
        string DeductibleStatus,
        string DeductionReason,
        string AccountingCategory);
}
