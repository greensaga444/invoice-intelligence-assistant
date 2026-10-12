namespace InvoiceIntelligence.Core.Downloader;

public sealed record OfficialInvoicePageRequest(
    string SessionToken,
    string TaxId,
    DateOnly DateFrom,
    DateOnly DateTo,
    InvoiceDirection Direction,
    int PageNumber,
    int PageSize,
    string? OfficialApiAppId = null,
    string? OfficialApiAccount = null,
    string? OfficialApiPassword = null);