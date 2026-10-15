using InvoiceIntelligence.Core.Processor;

namespace InvoiceIntelligence.Core.Tests.Processor;

public class LocalHeuristicLlmAnalyzerTests
{
    [Fact]
    public void Analyze_WhenItemNameMatchesEntertainment_ReturnsNonDeductibleDecision()
    {
        var analyzer = new LocalHeuristicLlmAnalyzer();
        var record = CreateRecord(itemName: "客戶招待餐敘", totalAmount: 1200m);

        var decision = analyzer.Analyze(record, []);

        Assert.Equal("不可抵稅", decision.DeductibleStatus);
        Assert.Equal("交際費", decision.AccountingCategory);
        Assert.Equal(0.84m, decision.ConfidenceScore);
    }

    [Fact]
    public void Analyze_WhenItemNameMatchesBusinessExpense_ReturnsDeductibleDecision()
    {
        var analyzer = new LocalHeuristicLlmAnalyzer();
        var record = CreateRecord(itemName: "軟體維護費", totalAmount: 1200m);

        var decision = analyzer.Analyze(record, []);

        Assert.Equal("可抵稅", decision.DeductibleStatus);
        Assert.Equal("營業費用", decision.AccountingCategory);
        Assert.Equal(0.82m, decision.ConfidenceScore);
    }

    [Fact]
    public void Analyze_WhenAmountIsHighAndNoKeyword_ReturnsManualReviewDecision()
    {
        var analyzer = new LocalHeuristicLlmAnalyzer();
        var record = CreateRecord(itemName: "不明專案費", totalAmount: 50000m);

        var decision = analyzer.Analyze(record, []);

        Assert.Equal("需人工覆核", decision.DeductibleStatus);
        Assert.Equal("待判定", decision.AccountingCategory);
        Assert.Equal(0.66m, decision.ConfidenceScore);
    }

    [Fact]
    public void Analyze_WhenNoKeywordAndNormalAmount_ReturnsDefaultDeductibleDecision()
    {
        var analyzer = new LocalHeuristicLlmAnalyzer();
        var record = CreateRecord(itemName: "一般行政支出", totalAmount: 2000m);

        var decision = analyzer.Analyze(record, []);

        Assert.Equal("可抵稅", decision.DeductibleStatus);
        Assert.Equal("營業費用", decision.AccountingCategory);
        Assert.Equal(0.74m, decision.ConfidenceScore);
    }

    [Fact]
    public void Analyze_WhenHighConfidenceReferenceExists_IncreasesConfidenceAndClampsToOne()
    {
        var analyzer = new LocalHeuristicLlmAnalyzer();
        var record = CreateRecord(itemName: "客戶招待餐敘", totalAmount: 2000m);
        var references =
            Enumerable.Range(1, 3)
                .Select(index => new RetrievedReferenceCase($"KB-{index:000}", "sample", "可抵稅", "資訊服務費", 0.91m))
                .ToArray();

        var decision = analyzer.Analyze(record, references);

        Assert.Equal(0.92m, decision.ConfidenceScore);
    }

    [Fact]
    public void Analyze_WithNullArguments_ThrowsArgumentNullException()
    {
        var analyzer = new LocalHeuristicLlmAnalyzer();

        Assert.Throws<ArgumentNullException>(() => analyzer.Analyze(null!, []));
        Assert.Throws<ArgumentNullException>(() => analyzer.Analyze(CreateRecord("雲端服務", 1000m), null!));
    }

    private static InvoiceAnalysisRecord CreateRecord(string itemName, decimal totalAmount)
    {
        var netAmount = decimal.Round(totalAmount / 1.05m, 2, MidpointRounding.AwayFromZero);
        var taxAmount = totalAmount - netAmount;

        return new InvoiceAnalysisRecord(
            InvoiceNumber: "INV-001",
            TaxId: "12345678",
            CompanyName: "Alpha Co",
            VendorTaxId: "87654321",
            ItemName: itemName,
            InvoiceDate: new DateOnly(2026, 1, 15),
            NetAmount: netAmount,
            TaxAmount: taxAmount,
            TotalAmount: totalAmount,
            SourceFileName: "source-invoice.xlsx");
    }
}
