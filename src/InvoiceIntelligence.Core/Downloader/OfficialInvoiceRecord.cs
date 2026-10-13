namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Normalized invoice/statistical record used across downloader workflows.
/// </summary>
public sealed record OfficialInvoiceRecord(
    string InvoiceNumber,
    DateOnly InvoiceDate,
    string SellerTaxId,
    string BuyerTaxId,
    decimal NetAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    InvoiceDirection Direction,
    string? Currency = null);