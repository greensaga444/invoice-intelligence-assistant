namespace InvoiceIntelligence.Core.Processor;

public sealed record InvoiceDecisionPipelineSummary(
    int TotalRecords,
    int ResolvedByRuleEngine,
    int ResolvedByCaseCache,
    int ResolvedByLlm);
