namespace InvoiceIntelligence.Core.Processor;

public sealed record InvoiceDecision(
    string DeductibleStatus,
    string DeductionReason,
    string AccountingCategory,
    decimal ConfidenceScore);
