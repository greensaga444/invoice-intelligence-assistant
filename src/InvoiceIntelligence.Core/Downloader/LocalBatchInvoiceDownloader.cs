using System.Globalization;
using ClosedXML.Excel;
using InvoiceIntelligence.Core.Configuration;

namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Local-mode downloader that generates deterministic sample invoice workbooks.
/// </summary>
public sealed class LocalBatchInvoiceDownloader : IBatchInvoiceDownloader
{
    public Task<BatchInvoiceDownloadResult> DownloadAsync(
        BatchInvoiceDownloadRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => DownloadCore(request, cancellationToken), cancellationToken);
    }

    private static BatchInvoiceDownloadResult DownloadCore(
        BatchInvoiceDownloadRequest request,
        CancellationToken cancellationToken)
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

        // Generate one workbook per customer so downstream files mimic real batch output shape.
        foreach (var customer in request.Customers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var fileName = OutputFileNameHelper.BuildFileName(
                    namingRule: request.OutputNamingRule,
                    taxId: customer.TaxId,
                    companyName: customer.CompanyName,
                    dateFrom: customer.DateFrom,
                    dateTo: customer.DateTo,
                    timestamp: batchTimestamp,
                    extension: ".xlsx");
                var outputPath = OutputFileNameHelper.EnsureUniquePath(Path.Combine(request.FolderAPath, fileName));

                WriteInvoiceWorkbook(outputPath, customer);

                itemResults.Add(new BatchInvoiceDownloadItemResult(
                    Customer: customer,
                    IsSuccess: true,
                    Message: "下載成功。",
                    OutputFilePath: outputPath,
                    OutputFilePaths: [outputPath]));
            }
            catch (Exception ex)
            {
                itemResults.Add(new BatchInvoiceDownloadItemResult(
                    Customer: customer,
                    IsSuccess: false,
                    Message: ex.Message,
                    OutputFilePath: null,
                    OutputFilePaths: null));
            }
        }

        var logPath = Path.Combine(request.FolderAPath, $"download-log-{batchTimestamp:yyyyMMdd-HHmmss}.log");
        WriteLogFile(logPath, itemResults, batchTimestamp);

        return new BatchInvoiceDownloadResult(itemResults, logPath);
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
            // Spread sample invoices across the requested date range with stable pseudo amounts.
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
            var outputs = result.OutputFilePaths is { Count: > 0 }
                ? string.Join(" | ", result.OutputFilePaths)
                : result.OutputFilePath ?? "無";
            logLines.Add($"輸出={outputs}");
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