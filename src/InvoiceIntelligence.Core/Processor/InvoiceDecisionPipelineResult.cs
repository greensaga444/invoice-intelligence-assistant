namespace InvoiceIntelligence.Core.Processor;

public sealed record InvoiceDecisionPipelineResult(
    IReadOnlyList<InvoiceDecisionResult> Items,
    InvoiceDecisionPipelineSummary Summary);
