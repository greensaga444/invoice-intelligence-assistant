namespace InvoiceIntelligence.Core.Downloader;

public sealed record OfficialInvoiceBatchDownloadRequest(
    string TaxId,
    DateOnly DateFrom,
    DateOnly DateTo,
    string SessionToken,
    bool IncludeSalesInvoices,
    bool IncludePurchaseInvoices,
    OfficialInvoiceDownloadOptions? Options = null);