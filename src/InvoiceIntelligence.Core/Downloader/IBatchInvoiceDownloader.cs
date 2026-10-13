namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Contract for batch downloader implementations (local mode and official mode).
/// </summary>
public interface IBatchInvoiceDownloader
{
    Task<BatchInvoiceDownloadResult> DownloadAsync(
        BatchInvoiceDownloadRequest request,
        CancellationToken cancellationToken = default);
}