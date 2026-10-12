using System.IO;
using ClosedXML.Excel;
using InvoiceIntelligence.Core.Downloader;

namespace InvoiceIntelligence.Core.Tests.Downloader;

public class LocalBatchInvoiceDownloaderTests
{
    [Fact]
    public void Download_ValidRequest_CreatesRawInvoiceFilesAndLog()
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
            var result = downloader.Download(request);

            Assert.Equal(2, result.Items.Count);
            Assert.All(result.Items, item => Assert.True(item.IsSuccess));
            Assert.All(result.Items, item => Assert.True(File.Exists(item.OutputFilePath)));
            Assert.True(File.Exists(result.LogFilePath));

            var firstOutputPath = result.Items[0].OutputFilePath;
            Assert.NotNull(firstOutputPath);

            using var workbook = new XLWorkbook(firstOutputPath);
            var sheet = workbook.Worksheet(1);

            Assert.Equal("統編", sheet.Cell(1, 1).GetString());
            Assert.Equal("公司名稱", sheet.Cell(1, 2).GetString());
            Assert.Equal("發票號碼", sheet.Cell(1, 3).GetString());
            Assert.Equal("12345678", sheet.Cell(2, 1).GetString());
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public void Download_EmptySessionToken_ThrowsInvalidOperationException()
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
            Assert.Throws<InvalidOperationException>(() => downloader.Download(request));
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public void Download_DuplicateGeneratedFileName_UsesUniqueFileNames()
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
            var result = downloader.Download(request);

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