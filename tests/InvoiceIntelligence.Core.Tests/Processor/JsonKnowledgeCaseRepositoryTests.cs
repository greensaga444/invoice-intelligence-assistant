using InvoiceIntelligence.Core.Processor;

namespace InvoiceIntelligence.Core.Tests.Processor;

public class JsonKnowledgeCaseRepositoryTests
{
    [Fact]
    public void SaveApprovedResults_WithApprovedRows_PersistsAndCanBeRetrievedByRag()
    {
        var repositoryFilePath = GetTempRepositoryPath();
        var repository = new JsonKnowledgeCaseRepository(repositoryFilePath);
        var approvedResult = CreateDecisionResult(itemName: "雲端授權服務", invoiceNumber: "INV-1001");

        try
        {
            var saveResult = repository.SaveApprovedResults(
                [approvedResult],
                approvedBy: "人工覆核",
                approvedAtUtc: new DateTimeOffset(2026, 10, 12, 8, 0, 0, TimeSpan.Zero));

            Assert.Equal(1, saveResult.AddedCount);
            Assert.Equal(0, saveResult.SkippedCount);
            Assert.True(File.Exists(repositoryFilePath));

            var knowledgeCases = repository.LoadCases();

            Assert.Single(knowledgeCases);
            Assert.StartsWith("KB-FB-20261012080000-", knowledgeCases[0].CaseId, StringComparison.Ordinal);
            Assert.Equal("可抵稅", knowledgeCases[0].DeductibleStatus);
            Assert.Contains("雲端授權服務", knowledgeCases[0].Keywords);

            var retriever = new KeywordRagRetriever(knowledgeCases);
            var references = retriever.Retrieve(CreateRecord(itemName: "雲端授權服務", invoiceNumber: "INV-LOOKUP"), maxResults: 3);

            Assert.Single(references);
            Assert.Equal(knowledgeCases[0].CaseId, references[0].CaseId);
        }
        finally
        {
            DeleteTempRepository(repositoryFilePath);
        }
    }

    [Fact]
    public void SaveApprovedResults_DuplicateRows_AreSkipped()
    {
        var repositoryFilePath = GetTempRepositoryPath();
        var repository = new JsonKnowledgeCaseRepository(repositoryFilePath);
        var approvedResult = CreateDecisionResult(itemName: "文具採購", invoiceNumber: "INV-2001");

        try
        {
            var firstSave = repository.SaveApprovedResults(
                [approvedResult],
                approvedBy: "人工覆核",
                approvedAtUtc: new DateTimeOffset(2026, 10, 12, 9, 0, 0, TimeSpan.Zero));

            var secondSave = repository.SaveApprovedResults(
                [approvedResult],
                approvedBy: "人工覆核",
                approvedAtUtc: new DateTimeOffset(2026, 10, 12, 9, 1, 0, TimeSpan.Zero));

            Assert.Equal(1, firstSave.AddedCount);
            Assert.Equal(0, firstSave.SkippedCount);
            Assert.Equal(0, secondSave.AddedCount);
            Assert.Equal(1, secondSave.SkippedCount);
            Assert.Single(repository.LoadCases());
        }
        finally
        {
            DeleteTempRepository(repositoryFilePath);
        }
    }

    [Fact]
    public void ApplyApprovedResults_NoSelection_DoesNotWriteKnowledgeRepository()
    {
        var repositoryFilePath = GetTempRepositoryPath();
        var repository = new JsonKnowledgeCaseRepository(repositoryFilePath);
        var service = new ManualKnowledgeFeedbackService(repository);

        try
        {
            var result = service.ApplyApprovedResults([], approvedBy: "人工覆核");

            Assert.Equal(0, result.AddedCount);
            Assert.Equal(0, result.SkippedCount);
            Assert.Equal(repositoryFilePath, result.RepositoryFilePath);
            Assert.False(File.Exists(repositoryFilePath));
        }
        finally
        {
            DeleteTempRepository(repositoryFilePath);
        }
    }

    private static InvoiceDecisionResult CreateDecisionResult(string itemName, string invoiceNumber)
    {
        return new InvoiceDecisionResult(
            Record: CreateRecord(itemName, invoiceNumber),
            Decision: new InvoiceDecision(
                DeductibleStatus: "可抵稅",
                DeductionReason: "人工覆核通過",
                AccountingCategory: "資訊服務費",
                ConfidenceScore: 0.94m),
            ResolvedBy: DecisionStage.Llm,
            ReferenceCases: []);
    }

    private static InvoiceAnalysisRecord CreateRecord(string itemName, string invoiceNumber)
    {
        return new InvoiceAnalysisRecord(
            InvoiceNumber: invoiceNumber,
            TaxId: "12345678",
            CompanyName: "Alpha Co",
            VendorTaxId: "87654321",
            ItemName: itemName,
            InvoiceDate: new DateOnly(2026, 1, 15),
            NetAmount: 1200m,
            TaxAmount: 60m,
            TotalAmount: 1260m,
            SourceFileName: "source.xlsx");
    }

    private static string GetTempRepositoryPath()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"knowledge-repo-{Guid.NewGuid():N}");
        return Path.Combine(tempDirectory, "knowledge-cases.json");
    }

    private static void DeleteTempRepository(string repositoryFilePath)
    {
        var directory = Path.GetDirectoryName(repositoryFilePath);
        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
