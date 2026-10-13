using System.Globalization;
using System.Text;

namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Writes official invoice records into UTF-8 BOM CSV for spreadsheet compatibility.
/// </summary>
public sealed class OfficialInvoiceCsvExporter : IOfficialInvoiceExporter
{
    public async Task ExportCsvAsync(
        IReadOnlyList<OfficialInvoiceRecord> invoices,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoices);

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("CSV 輸出路徑不可為空白。", nameof(outputPath));
        }

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = new FileStream(
            outputPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        await writer.WriteLineAsync("Direction,InvoiceNumber,InvoiceDate,SellerTaxId,BuyerTaxId,NetAmount,TaxAmount,TotalAmount,Currency");

        foreach (var invoice in invoices)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = string.Join(
                ',',
                EscapeCsv(invoice.Direction.ToString()),
                EscapeCsv(invoice.InvoiceNumber),
                EscapeCsv(invoice.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                EscapeCsv(invoice.SellerTaxId),
                EscapeCsv(invoice.BuyerTaxId),
                EscapeCsv(invoice.NetAmount.ToString("0.##", CultureInfo.InvariantCulture)),
                EscapeCsv(invoice.TaxAmount.ToString("0.##", CultureInfo.InvariantCulture)),
                EscapeCsv(invoice.TotalAmount.ToString("0.##", CultureInfo.InvariantCulture)),
                EscapeCsv(invoice.Currency ?? string.Empty));

            await writer.WriteLineAsync(line);
        }

        await writer.FlushAsync();
    }

    private static string EscapeCsv(string value)
    {
        // Quote fields only when required by RFC-style CSV escaping rules.
        if (!value.Contains(',')
            && !value.Contains('"')
            && !value.Contains('\n')
            && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}