using System.Globalization;
using System.Text;
using InvoiceIntelligence.Core.Configuration;

namespace InvoiceIntelligence.Core.Downloader;

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

    public BatchInvoiceDownloadResult Download(BatchInvoiceDownloadRequest request)
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

        foreach (var customer in request.Customers)
        {
            try
            {
                var queryRequest = new OfficialInvoiceBatchDownloadRequest(
                    TaxId: customer.TaxId,
                    DateFrom: customer.DateFrom,
                    DateTo: customer.DateTo,
                    SessionToken: request.SessionToken,
                    IncludeSalesInvoices: _includeSalesInvoices,
                    IncludePurchaseInvoices: _includePurchaseInvoices,
                    Options: _options);

                var queryResult = _officialDownloader.DownloadAsync(queryRequest).GetAwaiter().GetResult();

                var fileName = BuildFileName(request.OutputNamingRule, customer, batchTimestamp);
                var outputPath = EnsureUniquePath(Path.Combine(request.FolderAPath, fileName));

                _exporter.ExportCsvAsync(queryResult.Invoices, outputPath).GetAwaiter().GetResult();

                itemResults.Add(new BatchInvoiceDownloadItemResult(
                    Customer: customer,
                    IsSuccess: true,
                    Message: $"下載成功，筆數={queryResult.Invoices.Count}，API 呼叫={queryResult.TotalApiCalls}，重試={queryResult.TotalRetryCount}。",
                    OutputFilePath: outputPath));
            }
            catch (Exception ex)
            {
                itemResults.Add(new BatchInvoiceDownloadItemResult(
                    Customer: customer,
                    IsSuccess: false,
                    Message: ex.Message,
                    OutputFilePath: null));
            }
        }

        var logPath = Path.Combine(request.FolderAPath, $"download-log-{batchTimestamp:yyyyMMdd-HHmmss}.log");
        WriteLogFile(logPath, itemResults, batchTimestamp);

        return new BatchInvoiceDownloadResult(itemResults, logPath);
    }

    private static string BuildFileName(string namingRule, CustomerRecord customer, DateTimeOffset timestamp)
    {
        var fileName = namingRule
            .Replace("{taxId}", SanitizeSegment(customer.TaxId), StringComparison.OrdinalIgnoreCase)
            .Replace("{companyName}", SanitizeSegment(customer.CompanyName), StringComparison.OrdinalIgnoreCase)
            .Replace("{dateFrom}", customer.DateFrom.ToString("yyyyMMdd", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{dateTo}", customer.DateTo.ToString("yyyyMMdd", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{timestamp}", timestamp.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);

        if (!fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            fileName += ".csv";
        }

        return fileName;
    }

    private static string EnsureUniquePath(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return filePath;
        }

        var directory = Path.GetDirectoryName(filePath)
            ?? throw new InvalidOperationException("輸出路徑缺少目錄資訊。");
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);

        for (var index = 2; index <= 9999; index++)
        {
            var candidate = Path.Combine(directory, $"{fileNameWithoutExtension}-{index}{extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new IOException("無法產生唯一輸出檔名，請清理 Folder A 後重試。");
    }

    private static string SanitizeSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "未提供";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);

        foreach (var character in value.Trim())
        {
            if (invalid.Contains(character))
            {
                builder.Append('_');
                continue;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    private static void WriteLogFile(
        string logPath,
        IReadOnlyCollection<BatchInvoiceDownloadItemResult> itemResults,
        DateTimeOffset batchTimestamp)
    {
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
