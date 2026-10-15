namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Input parameters for one official API batch query.
/// </summary>
public sealed record OfficialInvoiceBatchDownloadRequest(
    string TaxId,
    DateOnly DateFrom,
    DateOnly DateTo,
    string SessionToken,
    bool IncludeSalesInvoices,
    bool IncludePurchaseInvoices,
    OfficialInvoiceDownloadOptions? Options = null,
    string? OfficialApiAppId = null,
    string? OfficialApiAccount = null,
    string? OfficialApiPassword = null,
    bool IncludeSalesAllowances = true,
    bool IncludePurchaseAllowances = true);