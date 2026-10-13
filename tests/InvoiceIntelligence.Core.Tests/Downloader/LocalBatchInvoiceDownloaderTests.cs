using System.IO;
using ClosedXML.Excel;
using InvoiceIntelligence.Core.Downloader;

namespace InvoiceIntelligence.Core.Tests.Downloader;

public class LocalBatchInvoiceDownloaderTests
{
    [Fact]
    public async Task DownloadAsync_ValidRequest_CreatesRawInvoiceFilesAndLog()
    {
        var outputDirectory = CreateTempDirectory();
        var customers = new[]
        {
            new CustomerRecord("12345678", "Alpha Co", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)),
            new CustomerRecord("87654321", "Beta Co", new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28))
        };

        var downloader = new LocalBatchInvoiceDownloader();
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
            Assert.All(result.Items, item => Assert.True(File.Exists(item.OutputFilePath)));
            Assert.True(File.Exists(result.LogFilePath));

            var firstOutputPath = result.Items[0].OutputFilePath;
            Assert.NotNull(firstOutputPath);

            using var workbook = new XLWorkbook(firstOutputPath);
            var sheet = workbook.Worksheet(1);
            var logLines = File.ReadAllLines(result.LogFilePath);

            Assert.Equal("原始發票", sheet.Name);
            Assert.Equal("統編", sheet.Cell(1, 1).GetString());
            Assert.Equal("公司名稱", sheet.Cell(1, 2).GetString());
            Assert.Equal("發票號碼", sheet.Cell(1, 3).GetString());
            Assert.Equal("12345678", sheet.Cell(2, 1).GetString());
            Assert.Equal("發票下載批次紀錄", logLines[0]);
            Assert.Contains("成功:", logLines[3]);
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public async Task DownloadAsync_EmptySessionToken_ThrowsInvalidOperationException()
    {
        var outputDirectory = CreateTempDirectory();
        var customers = new[]
        {
            new CustomerRecord("12345678", "Alpha Co", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31))
        };

        var downloader = new LocalBatchInvoiceDownloader();
        var request = new BatchInvoiceDownloadRequest(
            Customers: customers,
            FolderAPath: outputDirectory,
            OutputNamingRule: "{taxId}_{timestamp}.xlsx",
            SessionToken: string.Empty);

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => downloader.DownloadAsync(request));
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public async Task DownloadAsync_DuplicateGeneratedFileName_UsesUniqueFileNames()
    {
        var outputDirectory = CreateTempDirectory();
        var customers = new[]
        {
            new CustomerRecord("12345678", "Alpha Co", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)),
            new CustomerRecord("12345678", "Alpha Co", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31))
        };

        var downloader = new LocalBatchInvoiceDownloader();
        var request = new BatchInvoiceDownloadRequest(
            Customers: customers,
            FolderAPath: outputDirectory,
            OutputNamingRule: "{taxId}.xlsx",
            SessionToken: "session-token");

        try
        {
            var result = await downloader.DownloadAsync(request);

            Assert.Equal(2, result.Items.Count);
            Assert.All(result.Items, item => Assert.True(item.IsSuccess));

            var outputPaths = result.Items
                .Select(item => item.OutputFilePath)
                .Where(path => path is not null)
                .Cast<string>()
                .ToArray();

            Assert.Equal(2, outputPaths.Length);
            Assert.Equal(2, outputPaths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.All(outputPaths, path => Assert.True(File.Exists(path)));
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public async Task DownloadAsync_OneHundredFiftyCustomers_MeetsBatchScaleAndSuccessRateTarget()
    {
        var outputDirectory = CreateTempDirectory();
        var customers = Enumerable.Range(1, 150)
            .Select(index => new CustomerRecord(
                TaxId: $"{10000000 + index:D8}",
                CompanyName: $"Company {index:000}",
                DateFrom: new DateOnly(2026, 1, 1),
                DateTo: new DateOnly(2026, 1, 31)))
            .ToArray();

        var downloader = new LocalBatchInvoiceDownloader();
        var request = new BatchInvoiceDownloadRequest(
            Customers: customers,
            FolderAPath: outputDirectory,
            OutputNamingRule: "{taxId}_{timestamp}.xlsx",
            SessionToken: "session-token");

        try
        {
            var result = await downloader.DownloadAsync(request);
            var successCount = result.Items.Count(item => item.IsSuccess);
            var successRate = (decimal)successCount / result.Items.Count;

            Assert.Equal(150, result.Items.Count);
            Assert.True(successRate >= 0.99m, $"Success rate {successRate:P2} is below 99%.");
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"invoice-intelligence-tests-{Guid.NewGuid():N}");
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