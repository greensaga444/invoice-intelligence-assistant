using System.IO;
using ClosedXML.Excel;
using InvoiceIntelligence.Core.Downloader;

namespace InvoiceIntelligence.Core.Tests.Downloader;

public class OfficialInvoiceFolderAExcelExporterTests
{
    [Fact]
    public async Task ExportAsync_ValidInvoices_WritesOfficialTwoSheetWorkbook()
    {
        var outputDirectory = CreateTempDirectory();
        var outputPath = Path.Combine(outputDirectory, "official-invoices.xlsx");

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

        var exporter = new OfficialInvoiceFolderAExcelExporter();

        try
        {
            await exporter.ExportAsync(invoices, outputPath);

            Assert.True(File.Exists(outputPath));

            using var workbook = new XLWorkbook(outputPath);
            var summarySheet = workbook.Worksheet("btb411w_xls1");
            var detailSheet = workbook.Worksheet("btb411w_xls2");

            Assert.Equal("發票號碼", summarySheet.Cell(1, 1).GetString());
            Assert.Equal("買方統一編號", summarySheet.Cell(1, 6).GetString());
            Assert.Equal("營業稅", summarySheet.Cell(1, 15).GetString());
            Assert.Equal("AA12345678", summarySheet.Cell(2, 1).GetString());
            Assert.Equal("87654321", summarySheet.Cell(2, 6).GetString());
            Assert.Equal("12345678", summarySheet.Cell(2, 8).GetString());
            Assert.Equal(1050m, summarySheet.Cell(2, 16).GetValue<decimal>());

            Assert.Equal("發票號碼", detailSheet.Cell(1, 1).GetString());
            Assert.Equal("品名", detailSheet.Cell(1, 4).GetString());
            Assert.Equal("AA12345678", detailSheet.Cell(2, 1).GetString());
            Assert.Equal("銷項發票", detailSheet.Cell(2, 4).GetString());
            Assert.Equal(1050m, detailSheet.Cell(2, 8).GetValue<decimal>());
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public async Task ExportAsync_EmptyInvoices_WritesHeadersOnly()
    {
        var outputDirectory = CreateTempDirectory();
        var outputPath = Path.Combine(outputDirectory, "official-empty.xlsx");
        var exporter = new OfficialInvoiceFolderAExcelExporter();

        try
        {
            await exporter.ExportAsync(Array.Empty<OfficialInvoiceRecord>(), outputPath);

            using var workbook = new XLWorkbook(outputPath);
            var summarySheet = workbook.Worksheet("btb411w_xls1");
            var detailSheet = workbook.Worksheet("btb411w_xls2");

            Assert.Equal("發票號碼", summarySheet.Cell(1, 1).GetString());
            Assert.Equal("發票號碼", detailSheet.Cell(1, 1).GetString());
            Assert.False(summarySheet.Row(2).CellsUsed().Any());
            Assert.False(detailSheet.Row(2).CellsUsed().Any());
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"invoice-intelligence-official-exporter-tests-{Guid.NewGuid():N}");
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