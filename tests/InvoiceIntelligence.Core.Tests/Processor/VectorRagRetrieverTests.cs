using InvoiceIntelligence.Core.Processor;

namespace InvoiceIntelligence.Core.Tests.Processor;

public class VectorRagRetrieverTests
{
    [Fact]
    public void Constructor_NullKnowledgeCases_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new VectorRagRetriever(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_InvalidVectorDimensions_ThrowsArgumentOutOfRangeException(int vectorDimensions)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new VectorRagRetriever([], vectorDimensions: vectorDimensions));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Constructor_InvalidSimilarityThreshold_ThrowsArgumentOutOfRangeException(decimal minSimilarityScore)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new VectorRagRetriever([], minSimilarityScore: minSimilarityScore));
    }

    [Fact]
    public void Retrieve_NullRecord_ThrowsArgumentNullException()
    {
        var retriever = new VectorRagRetriever([]);

        Assert.Throws<ArgumentNullException>(() => retriever.Retrieve(null!, maxResults: 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Retrieve_InvalidMaxResults_ThrowsArgumentOutOfRangeException(int maxResults)
    {
        var retriever = new VectorRagRetriever([]);
        var record = CreateRecord(itemName: "雲端服務");

        Assert.Throws<ArgumentOutOfRangeException>(() => retriever.Retrieve(record, maxResults));
    }

    [Fact]
    public void Retrieve_WhitespaceItemName_ReturnsEmptyCollection()
    {
        var retriever = new VectorRagRetriever([
            new KnowledgeReferenceCase("KB-001", "雲端授權服務案例", "可抵稅", "資訊服務費", ["雲端", "授權"])
        ]);

        var result = retriever.Retrieve(CreateRecord(itemName: "   "), maxResults: 3);

        Assert.Empty(result);
    }

    [Fact]
    public void Retrieve_UsesSummaryAndKeywords_ReturnsMostSimilarCaseFirst()
    {
        var retriever = new VectorRagRetriever(
        [
            new KnowledgeReferenceCase("KB-003", "餐敘招待案例", "不可抵稅", "交際費", ["招待", "餐敘"]),
            new KnowledgeReferenceCase("KB-001", "雲端授權與維護費用案例", "可抵稅", "資訊服務費", ["雲端", "授權", "維護"]),
            new KnowledgeReferenceCase("KB-002", "一般顧問服務案例", "可抵稅", "營業費用", ["顧問", "服務"])
        ],
        vectorDimensions: 1024,
        minSimilarityScore: 0.05m);

        var result = retriever.Retrieve(CreateRecord(itemName: "雲端授權服務費"), maxResults: 3);

        Assert.NotEmpty(result);
        Assert.Equal("KB-001", result[0].CaseId);
        Assert.True(result[0].SimilarityScore >= result[^1].SimilarityScore);
    }

    [Fact]
    public void Retrieve_RespectsMaxResults()
    {
        var retriever = new VectorRagRetriever(
        [
            new KnowledgeReferenceCase("KB-001", "雲端授權案例", "可抵稅", "資訊服務費", ["雲端", "授權"]),
            new KnowledgeReferenceCase("KB-002", "雲端顧問案例", "可抵稅", "營業費用", ["雲端", "顧問"]),
            new KnowledgeReferenceCase("KB-003", "雲端維護案例", "可抵稅", "資訊服務費", ["雲端", "維護"])
        ],
        vectorDimensions: 1024,
        minSimilarityScore: 0.01m);

        var result = retriever.Retrieve(CreateRecord(itemName: "雲端授權與維護"), maxResults: 2);

        Assert.Equal(2, result.Count);
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