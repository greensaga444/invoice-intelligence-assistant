namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Query result payload and metrics returned from official API downloads.
/// </summary>
public sealed record OfficialInvoiceBatchDownloadResult(
    IReadOnlyList<OfficialInvoiceRecord> Invoices,
    int TotalApiCalls,
    int TotalRetryCount,
    int DeduplicatedCount);