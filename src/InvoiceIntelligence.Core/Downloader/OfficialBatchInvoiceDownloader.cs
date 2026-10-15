using InvoiceIntelligence.Core.Configuration;

namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Orchestrates official per-customer downloads plus Folder A Excel export and batch logging.
/// </summary>
public sealed class OfficialBatchInvoiceDownloader : IBatchInvoiceDownloader
{
    private readonly OfficialInvoiceBatchDownloader _officialDownloader;
    private readonly IOfficialInvoiceExporter _exporter;
    private readonly OfficialInvoiceDownloadOptions _options;
    private readonly OfficialInvoiceScopeSelection _scopeSelection;

    public OfficialBatchInvoiceDownloader(
        OfficialInvoiceBatchDownloader officialDownloader,
        IOfficialInvoiceExporter exporter,
        OfficialInvoiceDownloadOptions? options = null,
        OfficialInvoiceScopeSelection? scopeSelection = null)
    {
        _officialDownloader = officialDownloader ?? throw new ArgumentNullException(nameof(officialDownloader));
        _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
        _options = options ?? OfficialInvoiceDownloadOptions.Default;
        _scopeSelection = scopeSelection ?? OfficialInvoiceScopeSelection.Default;
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

        _scopeSelection.Validate();

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

            var invoices = queryResult.Invoices
                .Where(invoice => invoice.DocumentType == InvoiceDocumentType.Invoice)
                .ToArray();
            var allowances = queryResult.Invoices
                .Where(invoice => invoice.DocumentType == InvoiceDocumentType.Allowance)
                .ToArray();

            var outputPaths = new List<string>(2);

            if (_scopeSelection.IncludesAnyInvoice)
            {
                var invoiceOutputPath = BuildOutputPath(batchRequest, customer, batchTimestamp, batchRequest.InvoiceFileSuffix);
                await _exporter.ExportAsync(invoices, invoiceOutputPath, cancellationToken);
                outputPaths.Add(invoiceOutputPath);
            }

            if (_scopeSelection.IncludesAnyAllowance)
            {
                var allowanceOutputPath = BuildOutputPath(batchRequest, customer, batchTimestamp, batchRequest.AllowanceFileSuffix);
                await _exporter.ExportAsync(allowances, allowanceOutputPath, cancellationToken);
                outputPaths.Add(allowanceOutputPath);
            }

            return CreateSuccessResult(customer, queryResult, outputPaths);
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
                IncludeSalesInvoices: _scopeSelection.IncludeSalesInvoices,
                IncludePurchaseInvoices: _scopeSelection.IncludePurchaseInvoices,
            Options: _options,
            OfficialApiAppId: batchRequest.OfficialApiAppId,
            OfficialApiAccount: batchRequest.OfficialApiAccount,
            OfficialApiPassword: batchRequest.OfficialApiPassword,
                IncludeSalesAllowances: _scopeSelection.IncludeSalesAllowances,
                IncludePurchaseAllowances: _scopeSelection.IncludePurchaseAllowances);
    }

    private static string BuildOutputPath(
        BatchInvoiceDownloadRequest batchRequest,
        CustomerRecord customer,
        DateTimeOffset batchTimestamp,
        string fileSuffix)
    {
        var fileName = OutputFileNameHelper.BuildFileName(
            namingRule: batchRequest.OutputNamingRule,
            taxId: customer.TaxId,
            companyName: customer.CompanyName,
            dateFrom: customer.DateFrom,
            dateTo: customer.DateTo,
            timestamp: batchTimestamp,
            extension: ".xlsx");

        fileName = AppendFileCategoryBeforeExtension(fileName, fileSuffix);

        return OutputFileNameHelper.EnsureUniquePath(Path.Combine(batchRequest.FolderAPath, fileName));
    }

    private static string AppendFileCategoryBeforeExtension(string fileName, string fileSuffix)
    {
        if (string.IsNullOrWhiteSpace(fileSuffix))
        {
            return fileName;
        }

        var normalizedSuffix = fileSuffix.Trim().TrimStart('-');
        if (normalizedSuffix.Length == 0)
        {
            return fileName;
        }

        var extension = Path.GetExtension(fileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        return $"{nameWithoutExtension}-{normalizedSuffix}{extension}";
    }

    private static BatchInvoiceDownloadItemResult CreateSuccessResult(
        CustomerRecord customer,
        OfficialInvoiceBatchDownloadResult queryResult,
        IReadOnlyList<string> outputPaths)
    {
        var invoiceCount = queryResult.Invoices.Count(invoice => invoice.DocumentType == InvoiceDocumentType.Invoice);
        var allowanceCount = queryResult.Invoices.Count(invoice => invoice.DocumentType == InvoiceDocumentType.Allowance);

        return new BatchInvoiceDownloadItemResult(
            Customer: customer,
            IsSuccess: true,
            Message: $"下載成功，進銷項筆數={invoiceCount}，折讓筆數={allowanceCount}，API 呼叫={queryResult.TotalApiCalls}，重試={queryResult.TotalRetryCount}。",
            OutputFilePath: outputPaths.FirstOrDefault(),
            OutputFilePaths: outputPaths);
    }

    private static BatchInvoiceDownloadItemResult CreateFailureResult(CustomerRecord customer, string message)
    {
        return new BatchInvoiceDownloadItemResult(
            Customer: customer,
            IsSuccess: false,
            Message: message,
            OutputFilePath: null,
            OutputFilePaths: null);
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
            logLines.Add($"[{(result.IsSuccess ? "成功" : "失敗")}]");
            logLines.Add(
                $"統編={result.Customer.TaxId}, 公司={result.Customer.CompanyName}, 起日={result.Customer.DateFrom:yyyy-MM-dd}, 迄日={result.Customer.DateTo:yyyy-MM-dd}");
            logLines.Add($"訊息={result.Message}");
            var outputs = result.OutputFilePaths is { Count: > 0 }
                ? string.Join(" | ", result.OutputFilePaths)
                : result.OutputFilePath ?? "無";
            logLines.Add($"輸出={outputs}");
            logLines.Add(string.Empty);
        }

        File.WriteAllLines(logPath, logLines);
    }
}
