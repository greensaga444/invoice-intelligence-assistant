namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Aggregated batch outcome including per-customer results and log file path.
/// </summary>
public sealed record BatchInvoiceDownloadResult(
    IReadOnlyList<BatchInvoiceDownloadItemResult> Items,
    string LogFilePath);