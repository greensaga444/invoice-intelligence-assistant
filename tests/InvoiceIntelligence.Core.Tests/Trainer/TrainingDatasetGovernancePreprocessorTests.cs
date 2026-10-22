using InvoiceIntelligence.Core.Trainer;

namespace InvoiceIntelligence.Core.Tests.Trainer;

public class TrainingDatasetGovernancePreprocessorTests
{
    [Fact]
    public void Apply_WithExactDuplicateRows_RemovesDuplicates()
    {
        var preprocessor = new TrainingDatasetGovernancePreprocessor();
        var rows = new[]
        {
            CreateRow("公司A", new DateOnly(2026, 1, 1), "雲端授權", "可抵稅", "資訊服務費", "案例A"),
            CreateRow("  公司A ", new DateOnly(2026, 1, 1), " 雲端授權 ", "可抵稅", "資訊服務費", "案例A")
        };

        var result = preprocessor.Apply(rows);

        Assert.Single(result.ProcessedRecords);
        Assert.Equal(1, result.DuplicateCount);
        Assert.Equal(0, result.ConflictCount);
    }

    [Fact]
    public void Apply_WithCompetingLabels_ResolvesByMajority()
    {
        var preprocessor = new TrainingDatasetGovernancePreprocessor();
        var rows = new[]
        {
            CreateRow("公司A", new DateOnly(2026, 1, 1), "文具採購", "可抵稅", "辦公用品費", "案例A"),
            CreateRow("公司A", new DateOnly(2026, 1, 1), "文具採購", "可抵稅", "辦公用品費", "案例A"),
            CreateRow("公司A", new DateOnly(2026, 1, 1), "文具採購", "不可抵稅", "交際費", "案例B")
        };

        var result = preprocessor.Apply(rows);

        var selected = Assert.Single(result.ProcessedRecords);
        Assert.Equal("可抵稅", selected.DeductibleStatus);
        Assert.Equal("辦公用品費", selected.AccountingCategory);
        Assert.Equal("案例A", selected.DeductionReason);
        Assert.Equal(1, result.ConflictCount);
    }

    [Fact]
    public void Apply_WithLabelTie_UsesLexicalTieBreak()
    {
        var preprocessor = new TrainingDatasetGovernancePreprocessor();
        var rows = new[]
        {
            CreateRow("公司A", new DateOnly(2026, 1, 1), "文具採購", "不可抵稅", "交際費", "案例B"),
            CreateRow("公司A", new DateOnly(2026, 1, 1), "文具採購", "可抵稅", "辦公用品費", "案例A")
        };

        var result = preprocessor.Apply(rows);

        var selected = Assert.Single(result.ProcessedRecords);
        Assert.Equal("不可抵稅", selected.DeductibleStatus);
        Assert.Equal("交際費", selected.AccountingCategory);
        Assert.Equal("案例B", selected.DeductionReason);
    }

    private static TrainingDatasetRecord CreateRow(
        string companyName,
        DateOnly invoiceDate,
        string itemName,
        string deductibleStatus,
        string accountingCategory,
        string deductionReason)
    {
        return new TrainingDatasetRecord(
            CompanyName: companyName,
            InvoiceDate: invoiceDate,
            ItemName: itemName,
            DeductibleStatus: deductibleStatus,
            AccountingCategory: accountingCategory,
            DeductionReason: deductionReason);
    }
}