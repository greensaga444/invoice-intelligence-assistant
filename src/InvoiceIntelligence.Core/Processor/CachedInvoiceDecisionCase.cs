namespace InvoiceIntelligence.Core.Processor;

public sealed record CachedInvoiceDecisionCase(
    string VendorTaxId,
    string ItemName,
    decimal NetAmount,
    InvoiceDecision Decision);
