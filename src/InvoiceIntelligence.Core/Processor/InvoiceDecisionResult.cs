namespace InvoiceIntelligence.Core.Processor;

public sealed record InvoiceDecisionResult(
    InvoiceAnalysisRecord Record,
    InvoiceDecision Decision,
    DecisionStage ResolvedBy,
    IReadOnlyList<RetrievedReferenceCase> ReferenceCases);
