using System.IO;
using InvoiceIntelligence.Core.Downloader;

namespace InvoiceIntelligence.Core.Tests.Downloader;

public class OfficialBatchInvoiceDownloaderIntegrationTests
{
    [Fact]
    public void Download_ValidRequest_ExportsCsvAndCreatesLog()
    {
        var outputDirectory = CreateTempDirectory();
        var customers = new[]
        {
            new CustomerRecord("12345678", "Alpha Co", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31)),
            new CustomerRecord("87654321", "Beta Co", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31))
        };

        var apiClient = new FakeOfficialApiClient();
        var pagedDownloader = new OfficialInvoiceBatchDownloader(apiClient, (_, _) => Task.CompletedTask);
        var exporter = new OfficialInvoiceCsvExporter();
        var downloader = new OfficialBatchInvoiceDownloader(pagedDownloader, exporter);

        var request = new BatchInvoiceDownloadRequest(
            Customers: customers,
            FolderAPath: outputDirectory,
            OutputNamingRule: "{taxId}_{companyName}_{timestamp}.csv",
            SessionToken: "session-token");

        try
        {
            var result = downloader.Download(request);

            Assert.Equal(2, result.Items.Count);
            Assert.All(result.Items, item => Assert.True(item.IsSuccess));
            Assert.All(result.Items, item => Assert.NotNull(item.OutputFilePath));
            Assert.All(result.Items, item => Assert.True(File.Exists(item.OutputFilePath)));
            Assert.True(File.Exists(result.LogFilePath));

            var firstCsvPath = result.Items[0].OutputFilePath!;
            var csvLines = File.ReadAllLines(firstCsvPath);
            var logLines = File.ReadAllLines(result.LogFilePath);

            Assert.True(csvLines.Length >= 2);
            Assert.Equal("Direction,InvoiceNumber,InvoiceDate,SellerTaxId,BuyerTaxId,NetAmount,TaxAmount,TotalAmount,Currency", csvLines[0]);
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
