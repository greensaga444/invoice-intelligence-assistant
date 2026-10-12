using System.IO;
using InvoiceIntelligence.Core.Downloader;

namespace InvoiceIntelligence.Core.Tests.Downloader;

public class OfficialInvoiceCsvExporterTests
{
    [Fact]
    public async Task ExportCsvAsync_ValidInvoices_WritesHeaderAndRows()
    {
        var outputDirectory = CreateTempDirectory();
        var outputPath = Path.Combine(outputDirectory, "official-invoices.csv");

        var invoices = new[]
        {
            new OfficialInvoiceRecord(
                InvoiceNumber: "AA12345678",
                InvoiceDate: new DateOnly(2026, 9, 1),
                SellerTaxId: "12345678",
                BuyerTaxId: "87654321",
                NetAmount: 1000m,
                TaxAmount: 50m,
                TotalAmount: 1050m,
                Direction: InvoiceDirection.Sales,
                Currency: "TWD"),
            new OfficialInvoiceRecord(
                InvoiceNumber: "BB00000001",
                InvoiceDate: new DateOnly(2026, 9, 2),
                SellerTaxId: "22345678",
                BuyerTaxId: "97654321",
                NetAmount: 2000m,
                TaxAmount: 100m,
                TotalAmount: 2100m,
                Direction: InvoiceDirection.Purchase,
                Currency: "TWD")
        };

        var exporter = new OfficialInvoiceCsvExporter();

        try
        {
            await exporter.ExportCsvAsync(invoices, outputPath);

            Assert.True(File.Exists(outputPath));
            var lines = await File.ReadAllLinesAsync(outputPath);

            Assert.Equal(3, lines.Length);
            Assert.Equal("Direction,InvoiceNumber,InvoiceDate,SellerTaxId,BuyerTaxId,NetAmount,TaxAmount,TotalAmount,Currency", lines[0]);
            Assert.Contains("Sales,AA12345678,2026-09-01,12345678,87654321,1000,50,1050,TWD", lines[1]);
            Assert.Contains("Purchase,BB00000001,2026-09-02,22345678,97654321,2000,100,2100,TWD", lines[2]);
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public async Task ExportCsvAsync_RowContainsComma_EscapesWithDoubleQuotes()
    {
        var outputDirectory = CreateTempDirectory();
        var outputPath = Path.Combine(outputDirectory, "official-invoices.csv");

        var invoices = new[]
        {
            new OfficialInvoiceRecord(
                InvoiceNumber: "AA,12345678",
                InvoiceDate: new DateOnly(2026, 9, 1),
                SellerTaxId: "12345678",
                BuyerTaxId: "87654321",
                NetAmount: 1000m,
                TaxAmount: 50m,
                TotalAmount: 1050m,
                Direction: InvoiceDirection.Sales,
                Currency: "TWD")
        };

        var exporter = new OfficialInvoiceCsvExporter();

        try
        {
            await exporter.ExportCsvAsync(invoices, outputPath);

            var lines = await File.ReadAllLinesAsync(outputPath);
            Assert.Contains("\"AA,12345678\"", lines[1]);
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"invoice-intelligence-csv-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, true);
        }
    }
}