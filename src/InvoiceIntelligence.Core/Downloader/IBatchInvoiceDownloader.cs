namespace InvoiceIntelligence.Core.Downloader;

public interface IBatchInvoiceDownloader
{
    BatchInvoiceDownloadResult Download(BatchInvoiceDownloadRequest request);
}