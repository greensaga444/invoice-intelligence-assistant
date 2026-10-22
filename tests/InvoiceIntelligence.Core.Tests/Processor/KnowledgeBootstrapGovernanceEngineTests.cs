using InvoiceIntelligence.Core.Processor;

namespace InvoiceIntelligence.Core.Tests.Processor;

public class KnowledgeBootstrapGovernanceEngineTests
{
    [Fact]
    public void Resolve_WithDuplicateFingerprint_ReturnsSingleAcceptedRowAndDuplicateCount()
    {
        var engine = new KnowledgeBootstrapGovernanceEngine();
        var candidates = new[]
        {
            CreateCandidate("87654321", "  雲端   授權 ", "可抵稅", "資訊服務費", "案例A", new DateOnly(2026, 1, 10), KnowledgeLabelSource.GovernedBootstrapImport),
            CreateCandidate("87654321", "雲端 授權", "可抵稅", "資訊服務費", "案例A", new DateOnly(2026, 1, 12), KnowledgeLabelSource.GovernedBootstrapImport)
        };

        var result = engine.Resolve(candidates);

        Assert.Single(result.AcceptedRows);
        Assert.Equal(1, result.DuplicateCount);
        Assert.Equal(0, result.ConflictCount);
    }

    [Fact]
    public void Resolve_WithDifferentSources_PrefersManualApprovedFeedback()
    {
        var engine = new KnowledgeBootstrapGovernanceEngine();
        var candidates = new[]
        {
            CreateCandidate("87654321", "餐敘", "可抵稅", "營業費用", "舊標籤", new DateOnly(2026, 2, 10), KnowledgeLabelSource.GovernedBootstrapImport),
            CreateCandidate("87654321", "餐敘", "不可抵稅", "交際費", "人工覆核結論", new DateOnly(2026, 1, 10), KnowledgeLabelSource.ManualApprovedFeedback)
        };

        var result = engine.Resolve(candidates);

        var selected = Assert.Single(result.AcceptedRows);
        Assert.Equal("不可抵稅", selected.DeductibleStatus);
        Assert.Equal("交際費", selected.AccountingCategory);
        Assert.Equal("人工覆核結論", selected.DeductionReason);
        Assert.Equal(1, result.ConflictCount);
    }

    [Fact]
    public void Resolve_WithSameSource_PrefersNewestInvoiceDate()
    {
        var engine = new KnowledgeBootstrapGovernanceEngine();
        var candidates = new[]
        {
            CreateCandidate("87654321", "設備租賃", "可抵稅", "租賃費", "舊資料", new DateOnly(2026, 1, 5), KnowledgeLabelSource.GovernedBootstrapImport),
            CreateCandidate("87654321", "設備租賃", "不可抵稅", "交際費", "新資料", new DateOnly(2026, 3, 5), KnowledgeLabelSource.GovernedBootstrapImport)
        };

        var result = engine.Resolve(candidates);

        var selected = Assert.Single(result.AcceptedRows);
        Assert.Equal("不可抵稅", selected.DeductibleStatus);
        Assert.Equal("交際費", selected.AccountingCategory);
        Assert.Equal("新資料", selected.DeductionReason);
    }

    [Fact]
    public void Resolve_WithCompetingLabels_GeneratesConflictAudit()
    {
        var engine = new KnowledgeBootstrapGovernanceEngine();
        var candidates = new[]
        {
            CreateCandidate("87654321", "文具採購", "可抵稅", "辦公用品費", "案例A", new DateOnly(2026, 1, 5), KnowledgeLabelSource.GovernedBootstrapImport),
            CreateCandidate("87654321", "文具採購", "不可抵稅", "交際費", "案例B", new DateOnly(2026, 1, 5), KnowledgeLabelSource.GovernedBootstrapImport),
            CreateCandidate("87654321", "文具採購", "可抵稅", "辦公用品費", "案例A", new DateOnly(2026, 1, 5), KnowledgeLabelSource.GovernedBootstrapImport)
        };

        var result = engine.Resolve(candidates);

        Assert.Equal(1, result.ConflictCount);
        Assert.Equal(1, result.DuplicateCount);
        var conflict = Assert.Single(result.Conflicts);
        Assert.Equal("87654321|文具採購", conflict.ConflictKey);
        Assert.True(conflict.CompetingLabels.Count >= 2);
        Assert.Contains(conflict.CompetingLabels, label => label.Contains("可抵稅", StringComparison.Ordinal));
        Assert.Contains(conflict.CompetingLabels, label => label.Contains("不可抵稅", StringComparison.Ordinal));
    }

    private static KnowledgeBootstrapCandidate CreateCandidate(
        string vendorTaxId,
        string itemName,
        string deductibleStatus,
        string accountingCategory,
        string deductionReason,
        DateOnly invoiceDate,
        KnowledgeLabelSource source)
    {
        return new KnowledgeBootstrapCandidate(
            VendorTaxId: vendorTaxId,
            ItemName: itemName,
            DeductibleStatus: deductibleStatus,
            AccountingCategory: accountingCategory,
            DeductionReason: deductionReason,
            InvoiceDate: invoiceDate,
            Source: source);
    }
}