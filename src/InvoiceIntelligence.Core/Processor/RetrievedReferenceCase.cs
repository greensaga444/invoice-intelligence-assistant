namespace InvoiceIntelligence.Core.Processor;

public sealed record RetrievedReferenceCase(
    string CaseId,
    string Summary,
    string DeductibleStatus,
    string AccountingCategory,
    decimal SimilarityScore);
