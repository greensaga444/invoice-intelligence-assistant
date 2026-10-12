namespace InvoiceIntelligence.Core.Downloader;

public sealed class OfficialInvoiceBatchDownloader
{
    private readonly IMofOfficialInvoiceApiClient _apiClient;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;

    public OfficialInvoiceBatchDownloader(
        IMofOfficialInvoiceApiClient apiClient,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _delayAsync = delayAsync ?? Task.Delay;
    }

    public async Task<OfficialInvoiceBatchDownloadResult> DownloadAsync(
        OfficialInvoiceBatchDownloadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(request);
        var options = ValidateOptions(request.Options ?? OfficialInvoiceDownloadOptions.Default);

        var invoices = new List<OfficialInvoiceRecord>();
        var dedupKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var totalApiCalls = 0;
        var totalRetryCount = 0;
        var rawInvoiceCount = 0;

        foreach (var direction in ResolveDirections(request))
        {
            var pageNumber = 1;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (pageNumber > options.MaxPagesPerDirection)
                {
                    throw new InvalidOperationException(
                        $"{direction} 查詢超過單向最大頁數 {options.MaxPagesPerDirection}，請縮小區間或調整設定。");
                }

                var pageRequest = new OfficialInvoicePageRequest(
                    SessionToken: request.SessionToken,
                    TaxId: request.TaxId,
                    DateFrom: request.DateFrom,
                    DateTo: request.DateTo,
                    Direction: direction,
                    PageNumber: pageNumber,
                    PageSize: options.PageSize);

                var (pageResult, retryCount) = await QueryPageWithRetryAsync(pageRequest, options, cancellationToken);
                totalRetryCount += retryCount;
                totalApiCalls++;

                foreach (var invoice in pageResult.Items)
                {
                    rawInvoiceCount++;
                    var key = BuildDedupKey(invoice);
                    if (dedupKeys.Add(key))
                    {
                        invoices.Add(invoice);
                    }
                }

                if (!pageResult.HasMore || pageResult.Items.Count == 0)
                {
                    break;
                }

                pageNumber++;
            }
        }

        var deduplicatedCount = Math.Max(0, rawInvoiceCount - invoices.Count);

        return new OfficialInvoiceBatchDownloadResult(
            Invoices: invoices,
            TotalApiCalls: totalApiCalls,
            TotalRetryCount: totalRetryCount,
            DeduplicatedCount: deduplicatedCount);
    }

    private async Task<(OfficialInvoicePageResult PageResult, int RetryCount)> QueryPageWithRetryAsync(
        OfficialInvoicePageRequest pageRequest,
        OfficialInvoiceDownloadOptions options,
        CancellationToken cancellationToken)
    {
        var retryCount = 0;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var pageResult = await _apiClient.QueryInvoicesAsync(pageRequest, cancellationToken);
                return (pageResult, retryCount);
            }
            catch (MofOfficialInvoiceApiException ex) when (ex.IsTransient && attempt < options.MaxRetryAttempts)
            {
                retryCount++;
                var delay = CalculateBackoffDelay(attempt, options.RetryBaseDelay);
                await _delayAsync(delay, cancellationToken);
            }
        }
    }

    private static IEnumerable<InvoiceDirection> ResolveDirections(OfficialInvoiceBatchDownloadRequest request)
    {
        if (request.IncludeSalesInvoices)
        {
            yield return InvoiceDirection.Sales;
        }

        if (request.IncludePurchaseInvoices)
        {
            yield return InvoiceDirection.Purchase;
        }
    }

    private static void ValidateRequest(OfficialInvoiceBatchDownloadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TaxId))
        {
            throw new ArgumentException("統編不可為空白。", nameof(request));
        }

        if (request.DateTo < request.DateFrom)
        {
            throw new ArgumentException("查詢迄日不可早於起日。", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.SessionToken))
        {
            throw new InvalidOperationException("MOF 工作階段無效，請重新驗證後再查詢。");
        }

        if (!request.IncludeSalesInvoices && !request.IncludePurchaseInvoices)
        {
            throw new InvalidOperationException("請至少指定一種發票方向(進項或銷項)。");
        }
    }

    private static OfficialInvoiceDownloadOptions ValidateOptions(OfficialInvoiceDownloadOptions options)
    {
        if (options.PageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "PageSize 必須大於 0。");
        }

        if (options.MaxRetryAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxRetryAttempts 必須大於 0。");
        }

        if (options.RetryBaseDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "RetryBaseDelay 不可為負值。");
        }

        if (options.MaxPagesPerDirection <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxPagesPerDirection 必須大於 0。");
        }

        return options;
    }

    private static TimeSpan CalculateBackoffDelay(int attempt, TimeSpan baseDelay)
    {
        if (baseDelay == TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var factor = Math.Pow(2, attempt - 1);
        var delayMilliseconds = baseDelay.TotalMilliseconds * factor;

        return TimeSpan.FromMilliseconds(delayMilliseconds);
    }

    private static string BuildDedupKey(OfficialInvoiceRecord invoice)
    {
        return string.Join(
            '|',
            invoice.Direction.ToString(),
            invoice.InvoiceNumber,
            invoice.InvoiceDate.ToString("yyyyMMdd"),
            invoice.SellerTaxId,
            invoice.BuyerTaxId,
            invoice.TotalAmount.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
    }
}