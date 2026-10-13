namespace InvoiceIntelligence.Core.Downloader;

public interface IBatchInvoiceDownloader
{
    Task<BatchInvoiceDownloadResult> DownloadAsync(
        BatchInvoiceDownloadRequest request,
        CancellationToken cancellationToken = default);
}