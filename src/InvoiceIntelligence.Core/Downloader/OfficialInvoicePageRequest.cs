namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Low-level request model for one official API page query.
/// </summary>
public sealed record OfficialInvoicePageRequest(
    string SessionToken,
    string TaxId,
    DateOnly DateFrom,
    DateOnly DateTo,
    InvoiceDirection Direction,
    int PageNumber,
    int PageSize,
    InvoiceDocumentType DocumentType = InvoiceDocumentType.Invoice,
    string? OfficialApiAppId = null,
    string? OfficialApiAccount = null,
    string? OfficialApiPassword = null);