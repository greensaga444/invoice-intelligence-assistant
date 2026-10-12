namespace InvoiceIntelligence.Core.Processor;

public sealed record KnowledgeFeedbackUpdateResult(
    int AddedCount,
    int SkippedCount,
    string RepositoryFilePath);
