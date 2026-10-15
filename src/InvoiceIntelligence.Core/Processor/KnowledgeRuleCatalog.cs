namespace InvoiceIntelligence.Core.Processor;

public static class KnowledgeRuleCatalog
{
    public static IReadOnlyList<CachedInvoiceDecisionCase> DefaultCaseCache { get; } =
    [
        new CachedInvoiceDecisionCase(
            VendorTaxId: "65881234",
            ItemName: "雲端伺服器維護費",
            NetAmount: 12000m,
            Decision: new InvoiceDecision(
                DeductibleStatus: "可抵稅",
                DeductionReason: "案例快取命中：相同廠商與品名曾判定可抵稅。",
                AccountingCategory: "資訊服務費",
                ConfidenceScore: 0.93m)),
        new CachedInvoiceDecisionCase(
            VendorTaxId: "77124567",
            ItemName: "客戶招待餐敘",
            NetAmount: 5600m,
            Decision: new InvoiceDecision(
                DeductibleStatus: "不可抵稅",
                DeductionReason: "案例快取命中：交際招待費用不列抵。",
                AccountingCategory: "交際費",
                ConfidenceScore: 0.92m))
    ];

    public static IReadOnlyList<KnowledgeReferenceCase> DefaultKnowledgeCases { get; } =
    [
        new KnowledgeReferenceCase(
            CaseId: "KB-001",
            Summary: "文具採購通常歸屬辦公用品費並可抵稅。",
            DeductibleStatus: "可抵稅",
            AccountingCategory: "辦公用品費",
            Keywords: ["文具", "耗材", "影印紙"]),
        new KnowledgeReferenceCase(
            CaseId: "KB-002",
            Summary: "招待餐敘通常判定為交際費且不可抵稅。",
            DeductibleStatus: "不可抵稅",
            AccountingCategory: "交際費",
            Keywords: ["招待", "餐敘", "餐飲"]),
        new KnowledgeReferenceCase(
            CaseId: "KB-003",
            Summary: "資訊軟體與維護費通常可列為營業費用。",
            DeductibleStatus: "可抵稅",
            AccountingCategory: "資訊服務費",
            Keywords: ["軟體", "維護", "授權"])
    ];
}
