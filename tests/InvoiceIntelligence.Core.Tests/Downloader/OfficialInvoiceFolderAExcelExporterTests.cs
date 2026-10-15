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

    [Fact]
    public async Task ExportAsync_Raw1AllowanceItems_WritesAllowanceSheet()
    {
        var outputDirectory = CreateTempDirectory();
        var outputPath = Path.Combine(outputDirectory, "official-allowance.xlsx");

        var raw1Item = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["allowanceNo"] = "2026080718487658",
            ["formatCode"] = "23",
            ["allowanceStatus"] = "折讓已確認",
            ["allowanceType"] = "進項",
            ["invoiceNo"] = "DN24749905",
            ["invoiceDate"] = "2026-08-06",
            ["buyerTaxId"] = "54274765",
            ["buyerName"] = "立璽科技股份有限公司",
            ["sellerTaxId"] = "83513625",
            ["sellerName"] = "客遊天下玩樂股份有限公司",
            ["sendDate"] = "2026-08-07",
            ["itemName"] = "綜合服務",
            ["itemNetAmount"] = "357",
            ["itemTaxAmount"] = "18",
            ["netAmount"] = "357",
            ["taxAmount"] = "18",
            ["allowanceDate"] = "2026-08-07",
            ["lastUpdatedTime"] = "2026-08-07 19:05:43",
            ["migType"] = "G0401",
            ["senderTaxId"] = "97025978",
            ["senderName"] = "綠界科技股份有限公司"
        };

        var invoices = new[]
        {
            new OfficialInvoiceRecord(
                InvoiceNumber: "DN24749905",
                InvoiceDate: new DateOnly(2026, 8, 6),
                SellerTaxId: "83513625",
                BuyerTaxId: "54274765",
                NetAmount: 357m,
                TaxAmount: 18m,
                TotalAmount: 375m,
                Direction: InvoiceDirection.Purchase,
                DocumentType: InvoiceDocumentType.Allowance,
                Currency: "TWD",
                Raw1Items: [raw1Item])
        };

        var exporter = new OfficialInvoiceFolderAExcelExporter();

        try
        {
            await exporter.ExportAsync(invoices, outputPath);

            Assert.True(File.Exists(outputPath));

            using var workbook = new XLWorkbook(outputPath);
            var allowanceSheet = workbook.Worksheet("allowance");

            Assert.Equal("折讓單號碼", allowanceSheet.Cell(1, 1).GetString());
            Assert.Equal("品項名稱", allowanceSheet.Cell(1, 12).GetString());
            Assert.Equal("2026080718487658", allowanceSheet.Cell(2, 1).GetString());
            Assert.Equal("DN24749905", allowanceSheet.Cell(2, 5).GetString());
            Assert.Equal("綜合服務", allowanceSheet.Cell(2, 12).GetString());
            Assert.Equal(357m, allowanceSheet.Cell(2, 13).GetValue<decimal>());
            Assert.Equal(18m, allowanceSheet.Cell(2, 14).GetValue<decimal>());
            Assert.Equal("G0401", allowanceSheet.Cell(2, 20).GetString());
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