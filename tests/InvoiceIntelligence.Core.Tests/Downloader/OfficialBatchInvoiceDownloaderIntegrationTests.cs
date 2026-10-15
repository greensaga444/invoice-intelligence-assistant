using System.IO;
using ClosedXML.Excel;
using InvoiceIntelligence.Core.Downloader;

namespace InvoiceIntelligence.Core.Tests.Downloader;

public class OfficialBatchInvoiceDownloaderIntegrationTests
{
    [Fact]
    public async Task DownloadAsync_ValidRequest_ExportsWorkbookAndCreatesLog()
    {
        var outputDirectory = CreateTempDirectory();
        var customers = new[]
        {
            new CustomerRecord("12345678", "Alpha Co", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31)),
            new CustomerRecord("87654321", "Beta Co", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31))
        };

        var apiClient = new FakeOfficialApiClient();
        var pagedDownloader = new OfficialInvoiceBatchDownloader(apiClient, (_, _) => Task.CompletedTask);
        var exporter = new OfficialInvoiceFolderAExcelExporter();
        var downloader = new OfficialBatchInvoiceDownloader(pagedDownloader, exporter);

        var request = new BatchInvoiceDownloadRequest(
            Customers: customers,
            FolderAPath: outputDirectory,
            OutputNamingRule: "{taxId}_{companyName}_{timestamp}.xlsx",
            SessionToken: "session-token");

        try
        {
            var result = await downloader.DownloadAsync(request);

            Assert.Equal(2, result.Items.Count);
            Assert.All(result.Items, item => Assert.True(item.IsSuccess));
            Assert.All(result.Items, item => Assert.NotNull(item.OutputFilePaths));
            Assert.All(result.Items, item => Assert.Equal(2, item.OutputFilePaths!.Count));
            Assert.All(result.Items, item => Assert.All(item.OutputFilePaths!, path => Assert.True(File.Exists(path))));
            Assert.True(File.Exists(result.LogFilePath));

            var firstInvoiceWorkbookPath = result.Items[0].OutputFilePaths!
                .Single(path => Path.GetFileName(path).Contains("-invoice", StringComparison.OrdinalIgnoreCase));
            var firstAllowanceWorkbookPath = result.Items[0].OutputFilePaths!
                .Single(path => Path.GetFileName(path).Contains("-allowance", StringComparison.OrdinalIgnoreCase));

            var logLines = File.ReadAllLines(result.LogFilePath);
            using var invoiceWorkbook = new XLWorkbook(firstInvoiceWorkbookPath);
            using var allowanceWorkbook = new XLWorkbook(firstAllowanceWorkbookPath);
            var invoiceSummarySheet = invoiceWorkbook.Worksheet("btb411w_xls1");
            var invoiceDetailSheet = invoiceWorkbook.Worksheet("btb411w_xls2");
            var allowanceSummarySheet = allowanceWorkbook.Worksheet("btb411w_xls1");

            Assert.Equal("發票號碼", invoiceSummarySheet.Cell(1, 1).GetString());
            Assert.Equal("發票日期", invoiceSummarySheet.Cell(1, 5).GetString());
            Assert.Equal("發票號碼", invoiceDetailSheet.Cell(1, 1).GetString());
            Assert.Equal("品名", invoiceDetailSheet.Cell(1, 4).GetString());
            Assert.Equal("32", invoiceSummarySheet.Cell(2, 3).GetString());
            Assert.Equal("35", allowanceSummarySheet.Cell(2, 3).GetString());
            Assert.Contains("發票下載批次紀錄", logLines[0]);
            Assert.Contains("成功:", logLines[3]);
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    private sealed class FakeOfficialApiClient : IMofOfficialInvoiceApiClient
    {
        public Task<OfficialInvoicePageResult> QueryInvoicesAsync(OfficialInvoicePageRequest request, CancellationToken cancellationToken = default)
        {
            var invoiceNumber = request.Direction == InvoiceDirection.Sales
                ? $"S{request.TaxId}"
                : $"P{request.TaxId}";
            var totalAmount = request.Direction == InvoiceDirection.Sales ? 1050m : 2100m;
            var netAmount = decimal.Round(totalAmount / 1.05m, 2, MidpointRounding.AwayFromZero);
            var taxAmount = totalAmount - netAmount;

            var record = new OfficialInvoiceRecord(
                InvoiceNumber: invoiceNumber,
                InvoiceDate: request.DateFrom,
                SellerTaxId: request.Direction == InvoiceDirection.Sales ? request.TaxId : "11111111",
                BuyerTaxId: request.Direction == InvoiceDirection.Purchase ? request.TaxId : "22222222",
                NetAmount: netAmount,
                TaxAmount: taxAmount,
                TotalAmount: totalAmount,
                Direction: request.Direction,
                DocumentType: request.DocumentType,
                Currency: "TWD");

            return Task.FromResult(new OfficialInvoicePageResult([record], false));
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"invoice-intelligence-official-tests-{Guid.NewGuid():N}");
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
