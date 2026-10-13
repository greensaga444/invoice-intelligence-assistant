using InvoiceIntelligence.Core.Configuration;

namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Orchestrates official per-customer downloads plus CSV export and batch logging.
/// </summary>
public sealed class OfficialBatchInvoiceDownloader : IBatchInvoiceDownloader
{
    private readonly OfficialInvoiceBatchDownloader _officialDownloader;
    private readonly IOfficialInvoiceExporter _exporter;
    private readonly OfficialInvoiceDownloadOptions _options;
    private readonly bool _includeSalesInvoices;
    private readonly bool _includePurchaseInvoices;

    public OfficialBatchInvoiceDownloader(
        OfficialInvoiceBatchDownloader officialDownloader,
        IOfficialInvoiceExporter exporter,
        OfficialInvoiceDownloadOptions? options = null,
        bool includeSalesInvoices = true,
        bool includePurchaseInvoices = true)
    {
        _officialDownloader = officialDownloader ?? throw new ArgumentNullException(nameof(officialDownloader));
        _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
        _options = options ?? OfficialInvoiceDownloadOptions.Default;
        _includeSalesInvoices = includeSalesInvoices;
        _includePurchaseInvoices = includePurchaseInvoices;
    }

    public async Task<BatchInvoiceDownloadResult> DownloadAsync(
        BatchInvoiceDownloadRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Customers is null || request.Customers.Count == 0)
        {
            throw new InvalidOperationException("請先匯入至少一筆客戶資料，再執行批次下載。");
        }

        if (string.IsNullOrWhiteSpace(request.SessionToken))
        {
            throw new InvalidOperationException("MOF 工作階段無效，請重新驗證後再執行下載。");
        }

        if (string.IsNullOrWhiteSpace(request.FolderAPath))
        {
            throw new ArgumentException("資料夾 A 路徑不可為空白。", nameof(request));
        }

        if (!_includeSalesInvoices && !_includePurchaseInvoices)
        {
            throw new InvalidOperationException("官方下載模式至少需啟用進項或銷項其中一種方向。");
        }

        NamingRuleValidator.Validate(request.OutputNamingRule);

        Directory.CreateDirectory(request.FolderAPath);

        var batchTimestamp = DateTimeOffset.UtcNow;
        var itemResults = new List<BatchInvoiceDownloadItemResult>(request.Customers.Count);

        // Process each customer independently so one failure does not abort the whole batch.
        foreach (var customer in request.Customers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var itemResult = await DownloadCustomerAsync(request, customer, batchTimestamp, cancellationToken);
            itemResults.Add(itemResult);
        }

        var logPath = Path.Combine(request.FolderAPath, $"download-log-{batchTimestamp:yyyyMMdd-HHmmss}.log");
        WriteLogFile(logPath, itemResults, batchTimestamp);

        return new BatchInvoiceDownloadResult(itemResults, logPath);
    }

    private async Task<BatchInvoiceDownloadItemResult> DownloadCustomerAsync(
        BatchInvoiceDownloadRequest batchRequest,
        CustomerRecord customer,
        DateTimeOffset batchTimestamp,
        CancellationToken cancellationToken)
    {
        try
        {
            var queryRequest = BuildQueryRequest(batchRequest, customer);
            var queryResult = await _officialDownloader.DownloadAsync(queryRequest, cancellationToken);

            var outputPath = BuildOutputPath(batchRequest, customer, batchTimestamp);

            // Persist one CSV per customer to Folder A for downstream processing.
            await _exporter.ExportCsvAsync(queryResult.Invoices, outputPath, cancellationToken);

            return CreateSuccessResult(customer, queryResult, outputPath);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return CreateFailureResult(customer, ex.Message);
        }
    }

    private OfficialInvoiceBatchDownloadRequest BuildQueryRequest(
        BatchInvoiceDownloadRequest batchRequest,
        CustomerRecord customer)
    {
        // Combine per-customer date range with shared API credentials/options for one query run.
        return new OfficialInvoiceBatchDownloadRequest(
            TaxId: customer.TaxId,
            DateFrom: customer.DateFrom,
            DateTo: customer.DateTo,
            SessionToken: batchRequest.SessionToken,
            IncludeSalesInvoices: _includeSalesInvoices,
            IncludePurchaseInvoices: _includePurchaseInvoices,
            Options: _options,
            OfficialApiAppId: batchRequest.OfficialApiAppId,
            OfficialApiAccount: batchRequest.OfficialApiAccount,
            OfficialApiPassword: batchRequest.OfficialApiPassword);
    }

    private static string BuildOutputPath(
        BatchInvoiceDownloadRequest batchRequest,
        CustomerRecord customer,
        DateTimeOffset batchTimestamp)
    {
        var fileName = OutputFileNameHelper.BuildFileName(
            namingRule: batchRequest.OutputNamingRule,
            taxId: customer.TaxId,
            companyName: customer.CompanyName,
            dateFrom: customer.DateFrom,
            dateTo: customer.DateTo,
            timestamp: batchTimestamp,
            extension: ".csv");

        return OutputFileNameHelper.EnsureUniquePath(Path.Combine(batchRequest.FolderAPath, fileName));
    }

    private static BatchInvoiceDownloadItemResult CreateSuccessResult(
        CustomerRecord customer,
        OfficialInvoiceBatchDownloadResult queryResult,
        string outputPath)
    {
        return new BatchInvoiceDownloadItemResult(
            Customer: customer,
            IsSuccess: true,
            Message: $"下載成功，筆數={queryResult.Invoices.Count}，API 呼叫={queryResult.TotalApiCalls}，重試={queryResult.TotalRetryCount}。",
            OutputFilePath: outputPath);
    }

    private static BatchInvoiceDownloadItemResult CreateFailureResult(CustomerRecord customer, string message)
    {
        return new BatchInvoiceDownloadItemResult(
            Customer: customer,
            IsSuccess: false,
            Message: message,
            OutputFilePath: null);
    }

    private static void WriteLogFile(
        string logPath,
        IReadOnlyCollection<BatchInvoiceDownloadItemResult> itemResults,
        DateTimeOffset batchTimestamp)
    {
        // This log is intentionally human-readable so operators can trace each customer outcome quickly.
        var successCount = itemResults.Count(result => result.IsSuccess);
        var failureCount = itemResults.Count - successCount;

        var logLines = new List<string>
        {
            "發票下載批次紀錄",
            $"產生時間(UTC): {batchTimestamp:O}",
            $"總數: {itemResults.Count}",
            $"成功: {successCount}",
            $"失敗: {failureCount}",
            "---"
        };

        foreach (var result in itemResults)
        {
            logLines.Add($"[{(result.IsSuccess ? "成功" : "失敗")}]" );
            logLines.Add(
                $"統編={result.Customer.TaxId}, 公司={result.Customer.CompanyName}, 起日={result.Customer.DateFrom:yyyy-MM-dd}, 迄日={result.Customer.DateTo:yyyy-MM-dd}");
            logLines.Add($"訊息={result.Message}");
            logLines.Add($"輸出={result.OutputFilePath ?? "無"}");
            logLines.Add(string.Empty);
        }

        File.WriteAllLines(logPath, logLines);
    }
}
