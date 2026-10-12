namespace InvoiceIntelligence.Core.Downloader;

public sealed record OfficialInvoicePageResult(
    IReadOnlyList<OfficialInvoiceRecord> Items,
    bool HasMore);