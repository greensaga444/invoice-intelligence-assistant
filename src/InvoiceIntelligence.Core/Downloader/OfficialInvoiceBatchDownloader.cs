namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Executes official API query flow with paging, retry, and deduplication.
/// </summary>
public sealed class OfficialInvoiceBatchDownloader
{
    private sealed record QueryScope(InvoiceDirection Direction, InvoiceDocumentType DocumentType);

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

        // Query each enabled direction/type scope separately, then merge into one final result set.
        foreach (var scope in ResolveQueryScopes(request))
        {
            var hasMorePages = true;

            for (var pageNumber = 1; hasMorePages && pageNumber <= options.MaxPagesPerDirection; pageNumber++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pageRequest = BuildPageRequest(
                    request,
                    scope.Direction,
                    scope.DocumentType,
                    pageNumber,
                    options.PageSize);

                var (pageResult, retryCount) = await QueryPageWithRetryAsync(pageRequest, options, cancellationToken);
                totalRetryCount += retryCount;
                totalApiCalls++;

                rawInvoiceCount += MergeUniqueInvoices(pageResult.Items, dedupKeys, invoices);

                hasMorePages = pageResult.HasMore && pageResult.Items.Count > 0;
            }

            if (hasMorePages)
            {
                throw new InvalidOperationException(
                    $"{scope.Direction}/{scope.DocumentType} 查詢超過單向最大頁數 {options.MaxPagesPerDirection}，請縮小區間或調整設定。");
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
                // Exponential backoff reduces pressure on the API during temporary failures.
                retryCount++;
                var delay = CalculateBackoffDelay(attempt, options.RetryBaseDelay);
                await _delayAsync(delay, cancellationToken);
            }
        }
    }

    private static IEnumerable<QueryScope> ResolveQueryScopes(OfficialInvoiceBatchDownloadRequest request)
    {
        if (request.IncludeSalesInvoices)
        {
            yield return new QueryScope(InvoiceDirection.Sales, InvoiceDocumentType.Invoice);
        }

        if (request.IncludePurchaseInvoices)
        {
            yield return new QueryScope(InvoiceDirection.Purchase, InvoiceDocumentType.Invoice);
        }

        if (request.IncludeSalesAllowances)
        {
            yield return new QueryScope(InvoiceDirection.Sales, InvoiceDocumentType.Allowance);
        }

        if (request.IncludePurchaseAllowances)
        {
            yield return new QueryScope(InvoiceDirection.Purchase, InvoiceDocumentType.Allowance);
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

        if (!request.IncludeSalesInvoices
            && !request.IncludePurchaseInvoices
            && !request.IncludeSalesAllowances
            && !request.IncludePurchaseAllowances)
        {
            throw new InvalidOperationException("請至少指定一種查詢類型(銷項/進項發票、銷貨/進貨折讓)。");
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

    private static OfficialInvoicePageRequest BuildPageRequest(
        OfficialInvoiceBatchDownloadRequest request,
        InvoiceDirection direction,
        InvoiceDocumentType documentType,
        int pageNumber,
        int pageSize)
    {
        return new OfficialInvoicePageRequest(
            SessionToken: request.SessionToken,
            TaxId: request.TaxId,
            DateFrom: request.DateFrom,
            DateTo: request.DateTo,
            Direction: direction,
            PageNumber: pageNumber,
            PageSize: pageSize,
            DocumentType: documentType,
            OfficialApiAppId: request.OfficialApiAppId,
            OfficialApiAccount: request.OfficialApiAccount,
            OfficialApiPassword: request.OfficialApiPassword);
    }

    private static int MergeUniqueInvoices(
        IReadOnlyList<OfficialInvoiceRecord> pageItems,
        HashSet<string> dedupKeys,
        ICollection<OfficialInvoiceRecord> invoices)
    {
        var rawCount = 0;

        // Use a stable key to avoid duplicate rows when retries or upstream data overlap occur.
        foreach (var invoice in pageItems)
        {
            rawCount++;
            if (dedupKeys.Add(BuildDedupKey(invoice)))
            {
                invoices.Add(invoice);
            }
        }

        return rawCount;
    }

    private static string BuildDedupKey(OfficialInvoiceRecord invoice)
    {
        return string.Join(
            '|',
            invoice.Direction.ToString(),
            invoice.DocumentType.ToString(),
            invoice.InvoiceNumber,
            invoice.InvoiceDate.ToString("yyyyMMdd"),
            invoice.SellerTaxId,
            invoice.BuyerTaxId,
            invoice.TotalAmount.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
    }
}