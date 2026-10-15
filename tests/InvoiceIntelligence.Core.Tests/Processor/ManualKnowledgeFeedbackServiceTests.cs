using InvoiceIntelligence.Core.Processor;

namespace InvoiceIntelligence.Core.Tests.Processor;

public class ManualKnowledgeFeedbackServiceTests
{
    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ManualKnowledgeFeedbackService(null!));
    }

    [Fact]
    public void ApplyApprovedResults_NullResults_ThrowsArgumentNullException()
    {
        var repository = new SpyKnowledgeCaseRepository();
        var service = new ManualKnowledgeFeedbackService(repository);

        Assert.Throws<ArgumentNullException>(() => service.ApplyApprovedResults(null!, approvedBy: "qa"));
    }

    [Fact]
    public void ApplyApprovedResults_EmptyResults_DoesNotCallRepositorySave()
    {
        var repository = new SpyKnowledgeCaseRepository();
        var service = new ManualKnowledgeFeedbackService(repository);

        var result = service.ApplyApprovedResults([], approvedBy: "qa");

        Assert.Equal(0, repository.SaveCallCount);
        Assert.Equal(0, result.AddedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(repository.RepositoryFilePath, result.RepositoryFilePath);
    }

    [Fact]
    public void ApplyApprovedResults_WithApprovedRows_DelegatesToRepository()
    {
        var repository = new SpyKnowledgeCaseRepository
        {
            SaveResult = new KnowledgeFeedbackUpdateResult(AddedCount: 2, SkippedCount: 1, RepositoryFilePath: "repo.json")
        };

        var service = new ManualKnowledgeFeedbackService(repository);
        var approvedAt = new DateTimeOffset(2026, 10, 12, 8, 0, 0, TimeSpan.Zero);
        var approvedResults = new[] { CreateDecisionResult("INV-1001") };

        var result = service.ApplyApprovedResults(approvedResults, approvedBy: "qa", approvedAtUtc: approvedAt);

        Assert.Equal(1, repository.SaveCallCount);
        Assert.Same(approvedResults, repository.LastApprovedResults);
        Assert.Equal("qa", repository.LastApprovedBy);
        Assert.Equal(approvedAt, repository.LastApprovedAtUtc);
        Assert.Equal(repository.SaveResult, result);
    }

    private static InvoiceDecisionResult CreateDecisionResult(string invoiceNumber)
    {
        return new InvoiceDecisionResult(
            Record: new InvoiceAnalysisRecord(
                InvoiceNumber: invoiceNumber,
                TaxId: "12345678",
                CompanyName: "Alpha Co",
                VendorTaxId: "87654321",
                ItemName: "Cloud Service",
                InvoiceDate: new DateOnly(2026, 1, 15),
                NetAmount: 100m,
                TaxAmount: 5m,
                TotalAmount: 105m,
                SourceFileName: "source.xlsx"),
            Decision: new InvoiceDecision(
                DeductibleStatus: "可抵稅",
                DeductionReason: "人工覆核",
                AccountingCategory: "資訊服務費",
                ConfidenceScore: 0.90m),
            ResolvedBy: DecisionStage.Llm,
            ReferenceCases: []);
    }

    private sealed class SpyKnowledgeCaseRepository : IKnowledgeCaseRepository
    {
        public string RepositoryFilePath { get; init; } = "repo.json";

        public int SaveCallCount { get; private set; }

        public IReadOnlyCollection<InvoiceDecisionResult>? LastApprovedResults { get; private set; }

        public string? LastApprovedBy { get; private set; }

        public DateTimeOffset? LastApprovedAtUtc { get; private set; }

        public KnowledgeFeedbackUpdateResult SaveResult { get; set; } = new(0, 0, "repo.json");

        public IReadOnlyList<KnowledgeReferenceCase> LoadCases()
        {
            return [];
        }

        public KnowledgeFeedbackUpdateResult SaveApprovedResults(
            IReadOnlyCollection<InvoiceDecisionResult> approvedResults,
            string approvedBy,
            DateTimeOffset? approvedAtUtc = null)
        {
            SaveCallCount++;
            LastApprovedResults = approvedResults;
            LastApprovedBy = approvedBy;
            LastApprovedAtUtc = approvedAtUtc;
            return SaveResult;
        }
    }
}
