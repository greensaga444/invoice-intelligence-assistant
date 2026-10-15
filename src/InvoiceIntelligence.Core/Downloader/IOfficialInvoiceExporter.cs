namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Persists official invoice records into an output file format.
/// </summary>
public interface IOfficialInvoiceExporter
{
    Task ExportAsync(
        IReadOnlyList<OfficialInvoiceRecord> invoices,
        string outputPath,
        CancellationToken cancellationToken = default);
}