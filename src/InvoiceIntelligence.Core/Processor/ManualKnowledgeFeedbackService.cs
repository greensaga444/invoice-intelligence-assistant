namespace InvoiceIntelligence.Core.Processor;

public sealed class ManualKnowledgeFeedbackService
{
    private readonly IKnowledgeCaseRepository _knowledgeCaseRepository;

    public ManualKnowledgeFeedbackService(IKnowledgeCaseRepository knowledgeCaseRepository)
    {
        _knowledgeCaseRepository = knowledgeCaseRepository
            ?? throw new ArgumentNullException(nameof(knowledgeCaseRepository));
    }

    public KnowledgeFeedbackUpdateResult ApplyApprovedResults(
        IReadOnlyCollection<InvoiceDecisionResult> approvedResults,
        string approvedBy,
        DateTimeOffset? approvedAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(approvedResults);

        if (approvedResults.Count == 0)
        {
            return new KnowledgeFeedbackUpdateResult(
                AddedCount: 0,
                SkippedCount: 0,
                RepositoryFilePath: _knowledgeCaseRepository.RepositoryFilePath);
        }

        return _knowledgeCaseRepository.SaveApprovedResults(approvedResults, approvedBy, approvedAtUtc);
    }
}
