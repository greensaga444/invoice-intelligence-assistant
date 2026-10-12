namespace InvoiceIntelligence.Core.Processor;

public sealed record InvoiceAnalysisRecord(
    string InvoiceNumber,
    string TaxId,
    string CompanyName,
    string VendorTaxId,
    string ItemName,
    DateOnly InvoiceDate,
    decimal NetAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string SourceFileName);
