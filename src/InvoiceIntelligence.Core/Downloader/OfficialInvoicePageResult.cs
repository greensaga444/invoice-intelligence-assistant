namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Low-level response model for one official API page query.
/// </summary>
public sealed record OfficialInvoicePageResult(
    IReadOnlyList<OfficialInvoiceRecord> Items,
    bool HasMore);