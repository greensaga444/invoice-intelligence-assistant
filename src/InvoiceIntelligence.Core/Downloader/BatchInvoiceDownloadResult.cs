namespace InvoiceIntelligence.Core.Downloader;

public sealed record BatchInvoiceDownloadResult(
    IReadOnlyList<BatchInvoiceDownloadItemResult> Items,
    string LogFilePath);