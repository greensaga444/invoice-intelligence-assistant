using System.IO;
using System.Text;
using InvoiceIntelligence.Core.Downloader;

namespace InvoiceIntelligence.Core.Tests.Downloader;

public class OfficialBatchInvoiceDownloaderTests
{
    [Fact]
    public async Task DownloadAsync_EmptyCustomers_ThrowsInvalidOperationException()
    {
        var outputDirectory = CreateTempDirectory();
        var downloader = CreateDownloader(new StubOfficialApiClient(), new PassthroughExporter());

        try
        {
            var request = new BatchInvoiceDownloadRequest(
                Customers: [],
                FolderAPath: outputDirectory,
                OutputNamingRule: "{taxId}_{timestamp}.xlsx",
                SessionToken: "session-token");

            await Assert.ThrowsAsync<InvalidOperationException>(() => downloader.DownloadAsync(request));
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
        var downloader = CreateDownloader(new StubOfficialApiClient(), new PassthroughExporter());

        try
        {
            var request = BuildRequest(outputDirectory) with { SessionToken = " " };

            await Assert.ThrowsAsync<InvalidOperationException>(() => downloader.DownloadAsync(request));
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public async Task DownloadAsync_EmptyFolderAPath_ThrowsArgumentException()
    {
        var downloader = CreateDownloader(new StubOfficialApiClient(), new PassthroughExporter());
        var request = BuildRequest("C:\\unused") with { FolderAPath = " " };

        await Assert.ThrowsAsync<ArgumentException>(() => downloader.DownloadAsync(request));
    }

    [Fact]
    public async Task DownloadAsync_InvalidOutputNamingRule_ThrowsFormatException()
    {
        var outputDirectory = CreateTempDirectory();
        var downloader = CreateDownloader(new StubOfficialApiClient(), new PassthroughExporter());

        try
        {
            var request = BuildRequest(outputDirectory) with { OutputNamingRule = "{unknown}_{timestamp}.xlsx" };

            await Assert.ThrowsAsync<FormatException>(() => downloader.DownloadAsync(request));
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public async Task DownloadAsync_ScopeSelectionHasNoEnabledType_ThrowsInvalidOperationException()
    {
        var outputDirectory = CreateTempDirectory();
        var options = new OfficialInvoiceDownloadOptions(200, 2, TimeSpan.Zero, 50);

        var downloader = CreateDownloader(
            new StubOfficialApiClient(),
            new PassthroughExporter(),
            options,
            new OfficialInvoiceScopeSelection(
                IncludeSalesInvoices: false,
                IncludePurchaseInvoices: false,
                IncludeSalesAllowances: false,
                IncludePurchaseAllowances: false));

        try
        {
            var request = BuildRequest(outputDirectory);

            await Assert.ThrowsAsync<InvalidOperationException>(() => downloader.DownloadAsync(request));
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public async Task DownloadAsync_WhenSingleCustomerFails_ReturnsFailureButContinuesOtherCustomers()
    {
        var outputDirectory = CreateTempDirectory();
        var apiClient = new StubOfficialApiClient(failingTaxId: "99999999");
        var downloader = CreateDownloader(apiClient, new PassthroughExporter());

        var customers = new[]
        {
            new CustomerRecord("12345678", "Alpha Co", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31)),
            new CustomerRecord("99999999", "Failure Co", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31))
        };

        var request = BuildRequest(outputDirectory) with { Customers = customers };

        try
        {
            var result = await downloader.DownloadAsync(request);

            Assert.Equal(2, result.Items.Count);
            Assert.Equal(1, result.Items.Count(item => item.IsSuccess));
            Assert.Equal(1, result.Items.Count(item => !item.IsSuccess));
            Assert.True(File.Exists(result.LogFilePath));

            var log = File.ReadAllText(result.LogFilePath, Encoding.UTF8);
            Assert.Contains("成功", log, StringComparison.Ordinal);
            Assert.Contains("失敗", log, StringComparison.Ordinal);
            Assert.Contains("99999999", log, StringComparison.Ordinal);
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public async Task DownloadAsync_ConcurrentExecutions_ProduceDistinctOutputFiles()
    {
        var outputDirectory = CreateTempDirectory();
        var downloader = CreateDownloader(new StubOfficialApiClient(), new PassthroughExporter());
        var request = BuildRequest(outputDirectory);

        try
        {
            var tasks = new[]
            {
                downloader.DownloadAsync(request),
                downloader.DownloadAsync(request)
            };

            var results = await Task.WhenAll(tasks);
            var outputPaths = results
                .SelectMany(result => result.Items)
                .SelectMany(item => item.OutputFilePaths ?? [])
                .ToArray();

            Assert.NotEmpty(outputPaths);
            Assert.Equal(outputPaths.Length, outputPaths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.All(outputPaths, path => Assert.True(File.Exists(path)));
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public async Task DownloadAsync_WhenExporterFailsForOneCustomer_ContinuesOtherCustomers()
    {
        var outputDirectory = CreateTempDirectory();
        var exporter = new SelectiveFailingExporter("99999999");
        var downloader = CreateDownloader(new StubOfficialApiClient(), exporter);
        var request = BuildRequest(outputDirectory) with
        {
            Customers =
            [
                new CustomerRecord("12345678", "Alpha Co", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31)),
                new CustomerRecord("99999999", "Failure Co", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31))
            ]
        };

        try
        {
            var result = await downloader.DownloadAsync(request);

            Assert.Equal(2, result.Items.Count);
            Assert.Contains(result.Items, item => item.IsSuccess && item.Customer.TaxId == "12345678");
            Assert.Contains(result.Items, item => !item.IsSuccess && item.Customer.TaxId == "99999999");
            Assert.True(File.Exists(result.LogFilePath));
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public async Task DownloadAsync_LogFile_DoesNotContainOfficialApiCredentials()
    {
        var outputDirectory = CreateTempDirectory();
        var downloader = CreateDownloader(new StubOfficialApiClient(), new PassthroughExporter());
        var request = BuildRequest(outputDirectory) with
        {
            OfficialApiAppId = "app-id-should-not-appear",
            OfficialApiAccount = "merchant-account-should-not-appear",
            OfficialApiPassword = "secret-password-should-not-appear"
        };

        try
        {
            var result = await downloader.DownloadAsync(request);
            var log = File.ReadAllText(result.LogFilePath, Encoding.UTF8);

            Assert.DoesNotContain("app-id-should-not-appear", log, StringComparison.Ordinal);
            Assert.DoesNotContain("merchant-account-should-not-appear", log, StringComparison.Ordinal);
            Assert.DoesNotContain("secret-password-should-not-appear", log, StringComparison.Ordinal);
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    private static BatchInvoiceDownloadRequest BuildRequest(string outputDirectory)
    {
        return new BatchInvoiceDownloadRequest(
            Customers:
            [
                new CustomerRecord("12345678", "Alpha Co", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31))
            ],
            FolderAPath: outputDirectory,
            OutputNamingRule: "{taxId}_{companyName}_{timestamp}.xlsx",
            SessionToken: "session-token");
    }

    private static OfficialBatchInvoiceDownloader CreateDownloader(
        IMofOfficialInvoiceApiClient apiClient,
        IOfficialInvoiceExporter exporter,
        OfficialInvoiceDownloadOptions? options = null,
        OfficialInvoiceScopeSelection? scopeSelection = null)
    {
        var pagedDownloader = new OfficialInvoiceBatchDownloader(apiClient, (_, _) => Task.CompletedTask);
        return new OfficialBatchInvoiceDownloader(pagedDownloader, exporter, options, scopeSelection);
    }

    private sealed class StubOfficialApiClient(string? failingTaxId = null) : IMofOfficialInvoiceApiClient
    {
        public Task<OfficialInvoicePageResult> QueryInvoicesAsync(
            OfficialInvoicePageRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.Equals(request.TaxId, failingTaxId, StringComparison.OrdinalIgnoreCase))
            {
                throw new MofOfficialInvoiceApiException("forced failure", 500, isTransient: false);
            }

            var totalAmount = request.DocumentType == InvoiceDocumentType.Invoice ? 1050m : 315m;
            var netAmount = request.DocumentType == InvoiceDocumentType.Invoice ? 1000m : 300m;
            var taxAmount = totalAmount - netAmount;

            var record = new OfficialInvoiceRecord(
                InvoiceNumber: $"{request.TaxId}-{request.DocumentType}",
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

    private sealed class PassthroughExporter : IOfficialInvoiceExporter
    {
        public Task ExportAsync(
            IReadOnlyList<OfficialInvoiceRecord> invoices,
            string outputPath,
            CancellationToken cancellationToken = default)
        {
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return File.WriteAllTextAsync(outputPath, $"rows={invoices.Count}", cancellationToken);
        }
    }

    private sealed class SelectiveFailingExporter(string failingTaxId) : IOfficialInvoiceExporter
    {
        public Task ExportAsync(
            IReadOnlyList<OfficialInvoiceRecord> invoices,
            string outputPath,
            CancellationToken cancellationToken = default)
        {
            if (invoices.Any(invoice =>
                    invoice.InvoiceNumber.StartsWith(failingTaxId, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("forced exporter failure");
            }

            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return File.WriteAllTextAsync(outputPath, $"rows={invoices.Count}", cancellationToken);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"invoice-intelligence-batch-tests-{Guid.NewGuid():N}");
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
