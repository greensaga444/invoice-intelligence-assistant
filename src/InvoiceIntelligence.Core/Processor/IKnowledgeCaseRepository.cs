namespace InvoiceIntelligence.Core.Processor;

public interface IKnowledgeCaseRepository
{
    string RepositoryFilePath { get; }

    IReadOnlyList<KnowledgeReferenceCase> LoadCases();

    KnowledgeFeedbackUpdateResult SaveApprovedResults(
        IReadOnlyCollection<InvoiceDecisionResult> approvedResults,
        string approvedBy,
        DateTimeOffset? approvedAtUtc = null);
}
