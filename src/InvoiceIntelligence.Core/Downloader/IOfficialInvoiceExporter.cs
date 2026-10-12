namespace InvoiceIntelligence.Core.Downloader;

public interface IOfficialInvoiceExporter
{
    Task ExportCsvAsync(
        IReadOnlyList<OfficialInvoiceRecord> invoices,
        string outputPath,
        CancellationToken cancellationToken = default);
}