namespace InvoiceIntelligence.Core.Downloader;

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