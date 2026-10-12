namespace InvoiceIntelligence.Core.Downloader;

public sealed record OfficialInvoiceBatchDownloadResult(
    IReadOnlyList<OfficialInvoiceRecord> Invoices,
    int TotalApiCalls,
    int TotalRetryCount,
    int DeduplicatedCount);