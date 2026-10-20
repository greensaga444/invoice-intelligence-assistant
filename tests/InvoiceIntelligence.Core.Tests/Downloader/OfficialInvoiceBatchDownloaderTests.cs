using InvoiceIntelligence.Core.Downloader;

namespace InvoiceIntelligence.Core.Tests.Downloader;

public class OfficialInvoiceBatchDownloaderTests
{
    [Fact]
    public async Task DownloadAsync_EmptyTaxId_ThrowsArgumentException()
    {
        var downloader = new OfficialInvoiceBatchDownloader(
            new RecordingOfficialInvoiceApiClient(_ => Task.FromResult(new OfficialInvoicePageResult([], false))),
            (_, _) => Task.CompletedTask);

        var request = CreateRequest(taxId: " ");

        await Assert.ThrowsAsync<ArgumentException>(() => downloader.DownloadAsync(request));
    }

    [Fact]
    public async Task DownloadAsync_EndDateBeforeStartDate_ThrowsArgumentException()
    {
        var downloader = new OfficialInvoiceBatchDownloader(
            new RecordingOfficialInvoiceApiClient(_ => Task.FromResult(new OfficialInvoicePageResult([], false))),
            (_, _) => Task.CompletedTask);

        var request = CreateRequest(dateFrom: new DateOnly(2026, 8, 31), dateTo: new DateOnly(2026, 8, 1));

        await Assert.ThrowsAsync<ArgumentException>(() => downloader.DownloadAsync(request));
    }

    [Fact]
    public async Task DownloadAsync_EmptySessionToken_ThrowsInvalidOperationException()
    {
        var downloader = new OfficialInvoiceBatchDownloader(
            new RecordingOfficialInvoiceApiClient(_ => Task.FromResult(new OfficialInvoicePageResult([], false))),
            (_, _) => Task.CompletedTask);

        var request = CreateRequest(sessionToken: " ");

        await Assert.ThrowsAsync<InvalidOperationException>(() => downloader.DownloadAsync(request));
    }

    [Theory]
    [InlineData(0, 3, 10, 100)]
    [InlineData(200, 0, 10, 100)]
    [InlineData(200, 3, -10, 100)]
    [InlineData(200, 3, 10, 0)]
    public async Task DownloadAsync_InvalidOptions_ThrowsArgumentOutOfRangeException(
        int pageSize,
        int maxRetryAttempts,
        int retryBaseDelayMilliseconds,
        int maxPagesPerDirection)
    {
        var downloader = new OfficialInvoiceBatchDownloader(
            new RecordingOfficialInvoiceApiClient(_ => Task.FromResult(new OfficialInvoicePageResult([], false))),
            (_, _) => Task.CompletedTask);

        var options = new OfficialInvoiceDownloadOptions(
            PageSize: pageSize,
            MaxRetryAttempts: maxRetryAttempts,
            RetryBaseDelay: TimeSpan.FromMilliseconds(retryBaseDelayMilliseconds),
            MaxPagesPerDirection: maxPagesPerDirection);

        var request = CreateRequest(options: options);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => downloader.DownloadAsync(request));
    }

    [Fact]
    public async Task DownloadAsync_WhenHasMoreExceedsConfiguredMaxPages_ThrowsInvalidOperationException()
    {
        var client = new RecordingOfficialInvoiceApiClient(_ =>
            Task.FromResult(new OfficialInvoicePageResult(
                [CreateInvoice("AA12345678", new DateOnly(2026, 8, 1), InvoiceDirection.Sales, 1050m)],
                HasMore: true)));

        var downloader = new OfficialInvoiceBatchDownloader(client, (_, _) => Task.CompletedTask);
        var request = CreateRequest(options: new OfficialInvoiceDownloadOptions(200, 2, TimeSpan.Zero, 1));

        await Assert.ThrowsAsync<InvalidOperationException>(() => downloader.DownloadAsync(request));
        Assert.Single(client.Requests);
    }

    [Fact]
    public async Task DownloadAsync_NonTransientApiError_DoesNotRetry()
    {
        var delays = new List<TimeSpan>();
        var client = new RecordingOfficialInvoiceApiClient(_ =>
            throw new MofOfficialInvoiceApiException("fatal", 400, isTransient: false));

        var downloader = new OfficialInvoiceBatchDownloader(
            client,
            (delay, _) =>
            {
                delays.Add(delay);
                return Task.CompletedTask;
            });

        var request = CreateRequest(options: new OfficialInvoiceDownloadOptions(200, 3, TimeSpan.FromMilliseconds(50), 100));

        await Assert.ThrowsAsync<MofOfficialInvoiceApiException>(() => downloader.DownloadAsync(request));
        Assert.Empty(delays);
    }

    [Fact]
    public async Task DownloadAsync_TransientErrorWithZeroBaseDelay_RetriesWithZeroDelay()
    {
        var attempts = 0;
        var delays = new List<TimeSpan>();

        var client = new RecordingOfficialInvoiceApiClient(_ =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new MofOfficialInvoiceApiException("temporary", 503, isTransient: true);
            }

            return Task.FromResult(new OfficialInvoicePageResult(
            [
                CreateInvoice("RET-001", new DateOnly(2026, 8, 20), InvoiceDirection.Sales, 315m)
            ],
            HasMore: false));
        });

        var downloader = new OfficialInvoiceBatchDownloader(
            client,
            (delay, _) =>
            {
                delays.Add(delay);
                return Task.CompletedTask;
            });

        var request = CreateRequest(options: new OfficialInvoiceDownloadOptions(200, 3, TimeSpan.Zero, 100));

        var result = await downloader.DownloadAsync(request);

        Assert.Single(result.Invoices);
        Assert.Single(delays);
        Assert.Equal(TimeSpan.Zero, delays[0]);
        Assert.Equal(1, result.TotalRetryCount);
    }

    [Fact]
    public async Task DownloadAsync_MultiDirectionAndPaging_MergesResultsAndDeduplicates()
    {
        var client = new RecordingOfficialInvoiceApiClient(request =>
        {
            if (request.Direction == InvoiceDirection.Sales
                && request.DocumentType == InvoiceDocumentType.Invoice
                && request.PageNumber == 1)
            {
                return Task.FromResult(new OfficialInvoicePageResult(
                    Items:
                    [
                        CreateInvoice("AA12345678", new DateOnly(2026, 8, 1), InvoiceDirection.Sales, 1050m)
                    ],
                    HasMore: true));
            }

            if (request.Direction == InvoiceDirection.Sales
                && request.DocumentType == InvoiceDocumentType.Invoice
                && request.PageNumber == 2)
            {
                return Task.FromResult(new OfficialInvoicePageResult(
                    Items:
                    [
                        CreateInvoice("AA12345678", new DateOnly(2026, 8, 1), InvoiceDirection.Sales, 1050m)
                    ],
                    HasMore: false));
            }

            if (request.Direction == InvoiceDirection.Purchase
                && request.DocumentType == InvoiceDocumentType.Invoice
                && request.PageNumber == 1)
            {
                return Task.FromResult(new OfficialInvoicePageResult(
                    Items:
                    [
                        CreateInvoice("BB87654321", new DateOnly(2026, 8, 2), InvoiceDirection.Purchase, 2100m)
                    ],
                    HasMore: false));
            }

            throw new InvalidOperationException($"Unexpected request: {request.Direction}-{request.PageNumber}");
        });

        var downloader = new OfficialInvoiceBatchDownloader(client, (_, _) => Task.CompletedTask);
        var request = new OfficialInvoiceBatchDownloadRequest(
            TaxId: "12345678",
            DateFrom: new DateOnly(2026, 8, 1),
            DateTo: new DateOnly(2026, 9, 30),
            SessionToken: "session-token",
            IncludeSalesInvoices: true,
            IncludePurchaseInvoices: true,
            Options: new OfficialInvoiceDownloadOptions(200, 3, TimeSpan.FromMilliseconds(10), 100),
            IncludeSalesAllowances: false,
            IncludePurchaseAllowances: false);

        var result = await downloader.DownloadAsync(request);

        Assert.Equal(2, result.Invoices.Count);
        Assert.Equal(3, result.TotalApiCalls);
        Assert.Equal(1, result.DeduplicatedCount);
        Assert.Equal(3, client.Requests.Count);
        Assert.Contains(result.Invoices, invoice => invoice.Direction == InvoiceDirection.Sales);
        Assert.Contains(result.Invoices, invoice => invoice.Direction == InvoiceDirection.Purchase);
    }

    [Fact]
    public async Task DownloadAsync_AllFourScopesEnabled_QueriesEachDirectionAndDocumentType()
    {
        var client = new RecordingOfficialInvoiceApiClient(request =>
        {
            var invoiceNumber = request.Direction == InvoiceDirection.Sales
                ? request.DocumentType == InvoiceDocumentType.Invoice ? "S-INV" : "S-ALW"
                : request.DocumentType == InvoiceDocumentType.Invoice ? "P-INV" : "P-ALW";

            return Task.FromResult(new OfficialInvoicePageResult(
                Items:
                [
                    CreateInvoice(
                        invoiceNumber,
                        new DateOnly(2026, 8, 1),
                        request.Direction,
                        1000m,
                        request.DocumentType)
                ],
                HasMore: false));
        });

        var downloader = new OfficialInvoiceBatchDownloader(client, (_, _) => Task.CompletedTask);
        var request = new OfficialInvoiceBatchDownloadRequest(
            TaxId: "12345678",
            DateFrom: new DateOnly(2026, 8, 1),
            DateTo: new DateOnly(2026, 8, 31),
            SessionToken: "session-token",
            IncludeSalesInvoices: true,
            IncludePurchaseInvoices: true,
            Options: new OfficialInvoiceDownloadOptions(200, 3, TimeSpan.FromMilliseconds(10), 100),
            IncludeSalesAllowances: true,
            IncludePurchaseAllowances: true);

        var result = await downloader.DownloadAsync(request);

        Assert.Equal(4, result.TotalApiCalls);
        Assert.Equal(4, result.Invoices.Count);
        Assert.Contains(result.Invoices, invoice =>
            invoice.Direction == InvoiceDirection.Sales && invoice.DocumentType == InvoiceDocumentType.Invoice);
        Assert.Contains(result.Invoices, invoice =>
            invoice.Direction == InvoiceDirection.Purchase && invoice.DocumentType == InvoiceDocumentType.Invoice);
        Assert.Contains(result.Invoices, invoice =>
            invoice.Direction == InvoiceDirection.Sales && invoice.DocumentType == InvoiceDocumentType.Allowance);
        Assert.Contains(result.Invoices, invoice =>
            invoice.Direction == InvoiceDirection.Purchase && invoice.DocumentType == InvoiceDocumentType.Allowance);
    }

    [Fact]
    public async Task DownloadAsync_TransientError_RetriesAndSucceeds()
    {
        var salesPage1Attempts = 0;
        var delays = new List<TimeSpan>();

        var client = new RecordingOfficialInvoiceApiClient(request =>
        {
            if (request.Direction != InvoiceDirection.Sales
                || request.DocumentType != InvoiceDocumentType.Invoice
                || request.PageNumber != 1)
            {
                throw new InvalidOperationException("Unexpected request.");
            }

            salesPage1Attempts++;
            if (salesPage1Attempts == 1)
            {
                throw new MofOfficialInvoiceApiException("Temporary failure", 503, isTransient: true);
            }

            return Task.FromResult(new OfficialInvoicePageResult(
                Items:
                [
                    CreateInvoice("CC00000001", new DateOnly(2026, 8, 10), InvoiceDirection.Sales, 3150m)
                ],
                HasMore: false));
        });

        var downloader = new OfficialInvoiceBatchDownloader(
            client,
            (delay, _) =>
            {
                delays.Add(delay);
                return Task.CompletedTask;
            });

        var request = new OfficialInvoiceBatchDownloadRequest(
            TaxId: "12345678",
            DateFrom: new DateOnly(2026, 8, 1),
            DateTo: new DateOnly(2026, 9, 30),
            SessionToken: "session-token",
            IncludeSalesInvoices: true,
            IncludePurchaseInvoices: false,
            Options: new OfficialInvoiceDownloadOptions(200, 3, TimeSpan.FromMilliseconds(50), 100),
            IncludeSalesAllowances: false,
            IncludePurchaseAllowances: false);

        var result = await downloader.DownloadAsync(request);

        Assert.Single(result.Invoices);
        Assert.Equal(1, result.TotalRetryCount);
        Assert.Single(delays);
        Assert.Equal(TimeSpan.FromMilliseconds(50), delays[0]);
    }

    [Fact]
    public async Task DownloadAsync_TwoTransientErrors_UsesExponentialBackoff()
    {
        var attempts = 0;
        var delays = new List<TimeSpan>();
        var client = new RecordingOfficialInvoiceApiClient(_ =>
        {
            attempts++;
            if (attempts <= 2)
            {
                throw new MofOfficialInvoiceApiException("Temporary failure", 503, isTransient: true);
            }

            return Task.FromResult(new OfficialInvoicePageResult(
                [CreateInvoice("BACKOFF-001", new DateOnly(2026, 8, 10), InvoiceDirection.Sales, 3150m)],
                HasMore: false));
        });

        var downloader = new OfficialInvoiceBatchDownloader(
            client,
            (delay, _) =>
            {
                delays.Add(delay);
                return Task.CompletedTask;
            });

        var request = new OfficialInvoiceBatchDownloadRequest(
            TaxId: "12345678",
            DateFrom: new DateOnly(2026, 8, 1),
            DateTo: new DateOnly(2026, 9, 30),
            SessionToken: "session-token",
            IncludeSalesInvoices: true,
            IncludePurchaseInvoices: false,
            Options: new OfficialInvoiceDownloadOptions(200, 4, TimeSpan.FromMilliseconds(50), 100),
            IncludeSalesAllowances: false,
            IncludePurchaseAllowances: false);

        var result = await downloader.DownloadAsync(request);

        Assert.Single(result.Invoices);
        Assert.Equal(2, result.TotalRetryCount);
        Assert.Equal(2, delays.Count);
        Assert.Equal(TimeSpan.FromMilliseconds(50), delays[0]);
        Assert.Equal(TimeSpan.FromMilliseconds(100), delays[1]);
    }

    [Fact]
    public async Task DownloadAsync_TransientErrorExceedsMaxRetry_Throws()
    {
        var delays = new List<TimeSpan>();
        var client = new RecordingOfficialInvoiceApiClient(_ =>
            throw new MofOfficialInvoiceApiException("Temporary failure", 503, isTransient: true));

        var downloader = new OfficialInvoiceBatchDownloader(
            client,
            (delay, _) =>
            {
                delays.Add(delay);
                return Task.CompletedTask;
            });

        var request = new OfficialInvoiceBatchDownloadRequest(
            TaxId: "12345678",
            DateFrom: new DateOnly(2026, 8, 1),
            DateTo: new DateOnly(2026, 9, 30),
            SessionToken: "session-token",
            IncludeSalesInvoices: true,
            IncludePurchaseInvoices: false,
            Options: new OfficialInvoiceDownloadOptions(200, 2, TimeSpan.FromMilliseconds(25), 100),
            IncludeSalesAllowances: false,
            IncludePurchaseAllowances: false);

        await Assert.ThrowsAsync<MofOfficialInvoiceApiException>(() => downloader.DownloadAsync(request));
        Assert.Single(delays);
        Assert.Equal(TimeSpan.FromMilliseconds(25), delays[0]);
    }

    [Fact]
    public async Task DownloadAsync_NoDirectionSelected_ThrowsInvalidOperationException()
    {
        var client = new RecordingOfficialInvoiceApiClient(_ =>
            Task.FromResult(new OfficialInvoicePageResult([], false)));

        var downloader = new OfficialInvoiceBatchDownloader(client, (_, _) => Task.CompletedTask);

        var request = new OfficialInvoiceBatchDownloadRequest(
            TaxId: "12345678",
            DateFrom: new DateOnly(2026, 8, 1),
            DateTo: new DateOnly(2026, 9, 30),
            SessionToken: "session-token",
            IncludeSalesInvoices: false,
            IncludePurchaseInvoices: false,
            Options: null,
            IncludeSalesAllowances: false,
            IncludePurchaseAllowances: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => downloader.DownloadAsync(request));
    }

    private static OfficialInvoiceRecord CreateInvoice(
        string invoiceNumber,
        DateOnly invoiceDate,
        InvoiceDirection direction,
        decimal totalAmount,
        InvoiceDocumentType documentType = InvoiceDocumentType.Invoice)
    {
        var netAmount = decimal.Round(totalAmount / 1.05m, 2, MidpointRounding.AwayFromZero);
        var taxAmount = totalAmount - netAmount;

        return new OfficialInvoiceRecord(
            InvoiceNumber: invoiceNumber,
            InvoiceDate: invoiceDate,
            SellerTaxId: "12345678",
            BuyerTaxId: "87654321",
            NetAmount: netAmount,
            TaxAmount: taxAmount,
            TotalAmount: totalAmount,
            Direction: direction,
                DocumentType: documentType,
            Currency: "TWD");
    }

    private static OfficialInvoiceBatchDownloadRequest CreateRequest(
        string taxId = "12345678",
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null,
        string sessionToken = "session-token",
        bool includeSalesInvoices = true,
        bool includePurchaseInvoices = false,
        bool includeSalesAllowances = false,
        bool includePurchaseAllowances = false,
        OfficialInvoiceDownloadOptions? options = null)
    {
        return new OfficialInvoiceBatchDownloadRequest(
            TaxId: taxId,
            DateFrom: dateFrom ?? new DateOnly(2026, 8, 1),
            DateTo: dateTo ?? new DateOnly(2026, 8, 31),
            SessionToken: sessionToken,
            IncludeSalesInvoices: includeSalesInvoices,
            IncludePurchaseInvoices: includePurchaseInvoices,
            Options: options,
            IncludeSalesAllowances: includeSalesAllowances,
            IncludePurchaseAllowances: includePurchaseAllowances);
    }

    private sealed class RecordingOfficialInvoiceApiClient(
        Func<OfficialInvoicePageRequest, Task<OfficialInvoicePageResult>> handler)
        : IMofOfficialInvoiceApiClient
    {
        public List<OfficialInvoicePageRequest> Requests { get; } = [];

        public Task<OfficialInvoicePageResult> QueryInvoicesAsync(
            OfficialInvoicePageRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return handler(request);
        }
    }
}