using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using InvoiceIntelligence.Core.Configuration;

namespace InvoiceIntelligence.Core.Downloader;

public sealed class LocalBatchInvoiceDownloader : IBatchInvoiceDownloader
{
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

        NamingRuleValidator.Validate(request.OutputNamingRule);

        Directory.CreateDirectory(request.FolderAPath);

        var batchTimestamp = DateTimeOffset.UtcNow;
        var itemResults = new List<BatchInvoiceDownloadItemResult>(request.Customers.Count);

        foreach (var customer in request.Customers)
        {
            try
            {
                var fileName = BuildFileName(request.OutputNamingRule, customer, batchTimestamp);
                var outputPath = EnsureUniquePath(Path.Combine(request.FolderAPath, fileName));

                WriteInvoiceWorkbook(outputPath, customer);

                itemResults.Add(new BatchInvoiceDownloadItemResult(
                    Customer: customer,
                    IsSuccess: true,
                    Message: "下載成功。",
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

        if (!fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            fileName += ".xlsx";
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

    private static void WriteInvoiceWorkbook(string outputPath, CustomerRecord customer)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("原始發票");

        worksheet.Cell(1, 1).Value = "統編";
        worksheet.Cell(1, 2).Value = "公司名稱";
        worksheet.Cell(1, 3).Value = "發票號碼";
        worksheet.Cell(1, 4).Value = "發票日期";
        worksheet.Cell(1, 5).Value = "廠商統編";
        worksheet.Cell(1, 6).Value = "品名";
        worksheet.Cell(1, 7).Value = "未稅金額";
        worksheet.Cell(1, 8).Value = "稅額";
        worksheet.Cell(1, 9).Value = "含稅金額";

        var rows = CreateInvoiceRows(customer);

        for (var index = 0; index < rows.Count; index++)
        {
            var rowNumber = index + 2;
            var row = rows[index];

            worksheet.Cell(rowNumber, 1).Value = customer.TaxId;
            worksheet.Cell(rowNumber, 2).Value = customer.CompanyName;
            worksheet.Cell(rowNumber, 3).Value = row.InvoiceNo;
            worksheet.Cell(rowNumber, 4).Value = row.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            worksheet.Cell(rowNumber, 5).Value = row.VendorTaxId;
            worksheet.Cell(rowNumber, 6).Value = row.ItemName;
            worksheet.Cell(rowNumber, 7).Value = row.NetAmount;
            worksheet.Cell(rowNumber, 8).Value = row.TaxAmount;
            worksheet.Cell(rowNumber, 9).Value = row.TotalAmount;
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(outputPath);
    }

    private static List<InvoiceRow> CreateInvoiceRows(CustomerRecord customer)
    {
        var totalDays = Math.Max(1, customer.DateTo.DayNumber - customer.DateFrom.DayNumber + 1);
        var rowCount = Math.Min(3, totalDays);
        var step = Math.Max(1, totalDays / rowCount);
        var seed = int.Parse(customer.TaxId[^2..], CultureInfo.InvariantCulture);

        var rows = new List<InvoiceRow>(rowCount);

        for (var index = 0; index < rowCount; index++)
        {
            var dayOffset = Math.Min(totalDays - 1, index * step);
            var invoiceDate = customer.DateFrom.AddDays(dayOffset);
            var netAmount = 1000m + seed + (index * 250m);
            var taxAmount = decimal.Round(netAmount * 0.05m, 2, MidpointRounding.AwayFromZero);
            var totalAmount = netAmount + taxAmount;

            rows.Add(new InvoiceRow(
                InvoiceNo: $"INV-{customer.TaxId}-{invoiceDate:yyyyMMdd}-{index + 1:00}",
                InvoiceDate: invoiceDate,
                VendorTaxId: BuildVendorTaxId(customer.TaxId, index),
                ItemName: $"下載項目{index + 1}",
                NetAmount: netAmount,
                TaxAmount: taxAmount,
                TotalAmount: totalAmount));
        }

        return rows;
    }

    private static string BuildVendorTaxId(string taxId, int offset)
    {
        var prefix = int.Parse(taxId[..6], CultureInfo.InvariantCulture);
        var suffix = ((prefix + offset + 11) % 90) + 10;

        return $"{prefix:000000}{suffix:00}";
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
            logLines.Add(
                $"[{(result.IsSuccess ? "成功" : "失敗")}]"
            );
            logLines.Add(
                $"統編={result.Customer.TaxId}, 公司={result.Customer.CompanyName}, 起日={result.Customer.DateFrom:yyyy-MM-dd}, 迄日={result.Customer.DateTo:yyyy-MM-dd}");
            logLines.Add($"訊息={result.Message}");
            logLines.Add($"輸出={result.OutputFilePath ?? "無"}");
            logLines.Add(string.Empty);
        }

        File.WriteAllLines(logPath, logLines);
    }

    private sealed record InvoiceRow(
        string InvoiceNo,
        DateOnly InvoiceDate,
        string VendorTaxId,
        string ItemName,
        decimal NetAmount,
        decimal TaxAmount,
        decimal TotalAmount);
}