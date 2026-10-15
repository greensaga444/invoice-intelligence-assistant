using InvoiceIntelligence.Core.Processor;

namespace InvoiceIntelligence.Core.Tests.Processor;

public class KeywordRagRetrieverTests
{
    [Fact]
    public void Constructor_NullKnowledgeCases_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new KeywordRagRetriever(null!));
    }

    [Fact]
    public void Retrieve_NullRecord_ThrowsArgumentNullException()
    {
        var retriever = new KeywordRagRetriever([]);

        Assert.Throws<ArgumentNullException>(() => retriever.Retrieve(null!, maxResults: 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Retrieve_InvalidMaxResults_ThrowsArgumentOutOfRangeException(int maxResults)
    {
        var retriever = new KeywordRagRetriever([]);
        var record = CreateRecord(itemName: "Cloud Service");

        Assert.Throws<ArgumentOutOfRangeException>(() => retriever.Retrieve(record, maxResults));
    }

    [Fact]
    public void Retrieve_NoMatches_ReturnsEmptyCollection()
    {
        var retriever = new KeywordRagRetriever([
            new KnowledgeReferenceCase("CASE-1", "A", "可抵稅", "類別", ["stationery"])
        ]);

        var result = retriever.Retrieve(CreateRecord(itemName: "Cloud Service"), maxResults: 3);

        Assert.Empty(result);
    }

    [Fact]
    public void Retrieve_MatchesAreSortedBySimilarityThenCaseId()
    {
        var retriever = new KeywordRagRetriever([
            new KnowledgeReferenceCase("CASE-03", "high", "可抵稅", "資訊服務費", ["cloud", "license"]),
            new KnowledgeReferenceCase("CASE-02", "mid-2", "可抵稅", "資訊服務費", ["cloud", "consulting"]),
            new KnowledgeReferenceCase("CASE-01", "mid-1", "可抵稅", "資訊服務費", ["cloud", "support"])
        ]);

        var result = retriever.Retrieve(CreateRecord(itemName: "Cloud software license"), maxResults: 3);

        Assert.Equal(3, result.Count);
        Assert.Equal("CASE-03", result[0].CaseId);
        Assert.Equal(1.00m, result[0].SimilarityScore);
        Assert.Equal("CASE-01", result[1].CaseId);
        Assert.Equal(0.50m, result[1].SimilarityScore);
        Assert.Equal("CASE-02", result[2].CaseId);
        Assert.Equal(0.50m, result[2].SimilarityScore);
    }

    [Fact]
    public void Retrieve_WhitespaceKeyword_IsCountedInDenominatorForSimilarity()
    {
        var retriever = new KeywordRagRetriever([
            new KnowledgeReferenceCase("CASE-1", "summary", "可抵稅", "資訊服務費", ["cloud", " "])
        ]);

        var result = retriever.Retrieve(CreateRecord(itemName: "Cloud platform"), maxResults: 1);

        Assert.Single(result);
        Assert.Equal(0.50m, result[0].SimilarityScore);
    }

    private static InvoiceAnalysisRecord CreateRecord(string itemName)
    {
        return new InvoiceAnalysisRecord(
            InvoiceNumber: "INV-1001",
            TaxId: "12345678",
            CompanyName: "Alpha Co",
            VendorTaxId: "87654321",
            ItemName: itemName,
            InvoiceDate: new DateOnly(2026, 1, 15),
            NetAmount: 100m,
            TaxAmount: 5m,
            TotalAmount: 105m,
            SourceFileName: "source-invoice.xlsx");
    }
}
