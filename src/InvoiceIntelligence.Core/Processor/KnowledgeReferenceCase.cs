namespace InvoiceIntelligence.Core.Processor;

public sealed record KnowledgeReferenceCase(
    string CaseId,
    string Summary,
    string DeductibleStatus,
    string AccountingCategory,
    IReadOnlyList<string> Keywords);
