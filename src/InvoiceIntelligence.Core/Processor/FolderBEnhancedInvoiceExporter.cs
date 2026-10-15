using System.Globalization;
using ClosedXML.Excel;
using InvoiceIntelligence.Core.Configuration;

namespace InvoiceIntelligence.Core.Processor;

public sealed class FolderBEnhancedInvoiceExporter
{
    private const string DefaultInvoiceOutputLabel = "SalesInvoiceDetail-T3";
    private const string DefaultAllowanceOutputLabel = "PurchaseVoucherDetail-T4";

    private static readonly string[] SalesDetailHeaders =
    [
        "送件年月 [1]",
        "用戶代號 [2]",
        "所屬年月",
        "發票聯式 [3]",
        "第冊數 [4]",
        "發票號碼 [5]",
        "統一編號 [6]",
        "銷售額 [7]",
        "稅別 [8]",
        "稅額",
        "月日",
        "營業稅項目類別代號",
        "客戶代號 [9]",
        "公斤數",
        "列印註記&出售註記 [10]",
        "通關方式註記 [11]",
        "免稅銷售額",
        "借方項目 [12]",
        "借方摘要 [13]",
        "貸方項目 [14]",
        "貸方摘要 [15]",
        "稅額項目 [16]",
        "稅額摘要 [17]"
    ];

    private static readonly string[] PurchaseVoucherHeaders =
    [
        "送件年月 [1]",
        "用戶代號 [2]",
        "所屬年(起) [3]",
        "所屬月(起) [4]",
        "所屬月(迄) [5]",
        "憑證類別 [6]",
        "第冊數 [7]",
        "扣抵否 [8]",
        "序號(進項憑證) [9]",
        "類別(進項憑證) [10]",
        "憑證金額",
        "稅別 [11]",
        "稅額",
        "日期 [12]",
        "賣方統編 [13]",
        "營業稅項目類別代號 [14]",
        "供應商代號",
        "公斤數 [15]",
        "發票號碼&繳納證號碼 [16]",
        "年期別(公用事業憑證使用) [17]",
        "公用事業註記 [18]",
        "公用事業分攤註記 [19]",
        "借方項目 [20]",
        "借方摘要 [21]",
        "貸方項目 [22]",
        "貸方摘要 [23]",
        "稅額項目 [24]",
        "稅額摘要 [25]"
    ];

    public FolderBExportResult Export(
        IReadOnlyCollection<InvoiceDecisionResult> decisionResults,
        string folderBPath,
        string outputNamingRule,
        DateTimeOffset? exportTimestamp = null,
        string? invoiceOutputLabel = null,
        string? allowanceOutputLabel = null,
        string invoiceFileSuffix = "invoice",
        string allowanceFileSuffix = "allowance")
    {
        ArgumentNullException.ThrowIfNull(decisionResults);

        if (string.IsNullOrWhiteSpace(folderBPath))
        {
            throw new ArgumentException("資料夾 B 路徑不可為空白。", nameof(folderBPath));
        }

        NamingRuleValidator.Validate(outputNamingRule);

        if (decisionResults.Count == 0)
        {
            return new FolderBExportResult([]);
        }

        Directory.CreateDirectory(folderBPath);

        var timestamp = exportTimestamp ?? DateTimeOffset.UtcNow;
        var resolvedInvoiceOutputLabel = ResolveOutputLabel(invoiceOutputLabel, DefaultInvoiceOutputLabel);
        var resolvedAllowanceOutputLabel = ResolveOutputLabel(allowanceOutputLabel, DefaultAllowanceOutputLabel);
        var groupedResults = decisionResults
            .GroupBy(
                item => FolderAFileCategoryHelper.BuildGroupKey(
                    item.Record.SourceFileName,
                    invoiceFileSuffix,
                    allowanceFileSuffix),
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var outputFiles = new List<FolderBExportedFileResult>(groupedResults.Length * 2);

        foreach (var group in groupedResults)
        {
            var items = group
                .OrderBy(item => item.Record.InvoiceDate)
                .ThenBy(item => item.Record.InvoiceNumber, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (items.Length == 0)
            {
                continue;
            }

            var firstRecord = items[0].Record;
            var dateFrom = items.Min(item => item.Record.InvoiceDate);
            var dateTo = items.Max(item => item.Record.InvoiceDate);

            var outputFileName = OutputFileNameHelper.BuildFileName(
                namingRule: outputNamingRule,
                taxId: firstRecord.TaxId,
                companyName: firstRecord.CompanyName,
                dateFrom: dateFrom,
                dateTo: dateTo,
                timestamp: timestamp,
                extension: ".xlsx");

            var invoiceItems = items
                .Where(item =>
                {
                    var category = FolderAFileCategoryHelper.GetCategory(
                        item.Record.SourceFileName,
                        invoiceFileSuffix,
                        allowanceFileSuffix);
                    return category is FolderAFileCategory.Invoice or FolderAFileCategory.Unknown;
                })
                .ToArray();

            var allowanceItems = items
                .Where(item =>
                    FolderAFileCategoryHelper.GetCategory(
                        item.Record.SourceFileName,
                        invoiceFileSuffix,
                        allowanceFileSuffix) == FolderAFileCategory.Allowance)
                .ToArray();

            var sourceGroupName = string.IsNullOrWhiteSpace(group.Key)
                ? firstRecord.SourceFileName
                : group.Key;

            if (invoiceItems.Length > 0)
            {
                var outputPath = BuildTypedOutputPath(folderBPath, outputFileName, resolvedInvoiceOutputLabel);
                WriteSalesWorkbook(outputPath, invoiceItems);
                outputFiles.Add(new FolderBExportedFileResult(
                    SourceFileName: $"{sourceGroupName}-invoice",
                    OutputFilePath: outputPath,
                    RecordCount: invoiceItems.Length));
            }

            if (allowanceItems.Length > 0)
            {
                var outputPath = BuildTypedOutputPath(folderBPath, outputFileName, resolvedAllowanceOutputLabel);
                WritePurchaseWorkbook(outputPath, allowanceItems);
                outputFiles.Add(new FolderBExportedFileResult(
                    SourceFileName: $"{sourceGroupName}-allowance",
                    OutputFilePath: outputPath,
                    RecordCount: allowanceItems.Length));
            }
        }

        return new FolderBExportResult(outputFiles);
    }

    private static string BuildTypedOutputPath(string folderPath, string baseFileName, string tableSuffix)
    {
        var extension = Path.GetExtension(baseFileName);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseFileName);
        var typedFileName = $"{fileNameWithoutExtension}-{SanitizeOutputLabel(tableSuffix)}{extension}";
        return OutputFileNameHelper.EnsureUniquePath(Path.Combine(folderPath, typedFileName));
    }

    private static string ResolveOutputLabel(string? configuredLabel, string fallback)
    {
        return string.IsNullOrWhiteSpace(configuredLabel)
            ? fallback
            : configuredLabel.Trim();
    }

    private static string SanitizeOutputLabel(string label)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var buffer = label.ToCharArray();

        for (var index = 0; index < buffer.Length; index++)
        {
            if (invalidCharacters.Contains(buffer[index]))
            {
                buffer[index] = '_';
            }
        }

        return new string(buffer);
    }

    private static void WriteSalesWorkbook(string outputPath, IReadOnlyList<InvoiceDecisionResult> items)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("銷項發票明細檔(表三)");

        WriteHeaders(worksheet, SalesDetailHeaders);

        for (var index = 0; index < items.Count; index++)
        {
            var rowNumber = index + 2;
            var item = items[index];
            var record = item.Record;
            var decision = item.Decision;

            var taxType = ResolveTaxType(record);
            worksheet.Cell(rowNumber, 1).Value = ToRocYearMonth(record.InvoiceDate);
            worksheet.Cell(rowNumber, 2).Value = record.TaxId;
            worksheet.Cell(rowNumber, 3).Value = ToRocYearMonth(record.InvoiceDate);
            worksheet.Cell(rowNumber, 4).Value = "7";
            worksheet.Cell(rowNumber, 5).Value = "00001";
            worksheet.Cell(rowNumber, 6).Value = record.InvoiceNumber;
            worksheet.Cell(rowNumber, 7).Value = record.VendorTaxId;
            worksheet.Cell(rowNumber, 8).Value = record.NetAmount;
            worksheet.Cell(rowNumber, 9).Value = taxType;
            worksheet.Cell(rowNumber, 10).Value = record.TaxAmount;
            worksheet.Cell(rowNumber, 11).Value = record.InvoiceDate.ToString("MMdd", CultureInfo.InvariantCulture);
            worksheet.Cell(rowNumber, 12).Value = "1001";
            worksheet.Cell(rowNumber, 13).Value = record.TaxId;
            worksheet.Cell(rowNumber, 14).Value = string.Empty;
            worksheet.Cell(rowNumber, 15).Value = string.Empty;
            worksheet.Cell(rowNumber, 16).Value = string.Empty;
            worksheet.Cell(rowNumber, 17).Value = taxType == "*" ? record.NetAmount : 0m;
            worksheet.Cell(rowNumber, 18).Value = string.Empty;
            worksheet.Cell(rowNumber, 19).Value = decision.AccountingCategory;
            worksheet.Cell(rowNumber, 20).Value = string.Empty;
            worksheet.Cell(rowNumber, 21).Value = decision.DeductionReason;
            worksheet.Cell(rowNumber, 22).Value = string.Empty;
            worksheet.Cell(rowNumber, 23).Value = $"{decision.DeductibleStatus}/{decision.AccountingCategory}";
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(outputPath);
    }

    private static void WritePurchaseWorkbook(string outputPath, IReadOnlyList<InvoiceDecisionResult> items)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("進項憑證明細檔(表四)");

        WriteHeaders(worksheet, PurchaseVoucherHeaders);

        for (var index = 0; index < items.Count; index++)
        {
            var rowNumber = index + 2;
            var sequenceNumber = index % 100;
            var item = items[index];
            var record = item.Record;
            var decision = item.Decision;

            worksheet.Cell(rowNumber, 1).Value = ToRocYearMonth(record.InvoiceDate);
            worksheet.Cell(rowNumber, 2).Value = record.TaxId;
            worksheet.Cell(rowNumber, 3).Value = ToRocYear(record.InvoiceDate);
            worksheet.Cell(rowNumber, 4).Value = record.InvoiceDate.Month.ToString("00", CultureInfo.InvariantCulture);
            worksheet.Cell(rowNumber, 5).Value = record.InvoiceDate.Month.ToString("00", CultureInfo.InvariantCulture);
            worksheet.Cell(rowNumber, 6).Value = "1";
            worksheet.Cell(rowNumber, 7).Value = "0001";
            worksheet.Cell(rowNumber, 8).Value = ResolveDeductibleFlag(decision.DeductibleStatus);
            worksheet.Cell(rowNumber, 9).Value = sequenceNumber;
            worksheet.Cell(rowNumber, 10).Value = ResolveVoucherCategory(decision.AccountingCategory);
            worksheet.Cell(rowNumber, 11).Value = record.NetAmount;
            worksheet.Cell(rowNumber, 12).Value = ResolveTaxType(record);
            worksheet.Cell(rowNumber, 13).Value = record.TaxAmount;
            worksheet.Cell(rowNumber, 14).Value = ToRocDate(record.InvoiceDate);
            worksheet.Cell(rowNumber, 15).Value = record.VendorTaxId;
            worksheet.Cell(rowNumber, 16).Value = "1001";
            worksheet.Cell(rowNumber, 17).Value = record.VendorTaxId;
            worksheet.Cell(rowNumber, 18).Value = string.Empty;
            worksheet.Cell(rowNumber, 19).Value = record.InvoiceNumber;
            worksheet.Cell(rowNumber, 20).Value = string.Empty;
            worksheet.Cell(rowNumber, 21).Value = "N";
            worksheet.Cell(rowNumber, 22).Value = string.Empty;
            worksheet.Cell(rowNumber, 23).Value = string.Empty;
            worksheet.Cell(rowNumber, 24).Value = decision.AccountingCategory;
            worksheet.Cell(rowNumber, 25).Value = string.Empty;
            worksheet.Cell(rowNumber, 26).Value = decision.DeductionReason;
            worksheet.Cell(rowNumber, 27).Value = string.Empty;
            worksheet.Cell(rowNumber, 28).Value = decision.DeductibleStatus;
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(outputPath);
    }

    private static void WriteHeaders(IXLWorksheet worksheet, IReadOnlyList<string> headers)
    {
        for (var index = 0; index < headers.Count; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
        }
    }

    private static string ResolveTaxType(InvoiceAnalysisRecord record)
    {
        return record.TaxAmount > 0m ? "5" : "*";
    }

    private static string ResolveDeductibleFlag(string deductibleStatus)
    {
        if (deductibleStatus.Contains("不可", StringComparison.Ordinal))
        {
            return "N";
        }

        if (deductibleStatus.Contains("可抵", StringComparison.Ordinal))
        {
            return "Y";
        }

        return "N";
    }

    private static string ResolveVoucherCategory(string accountingCategory)
    {
        if (accountingCategory.Contains("固定", StringComparison.Ordinal)
            || accountingCategory.Contains("設備", StringComparison.Ordinal)
            || accountingCategory.Contains("資產", StringComparison.Ordinal))
        {
            return "3";
        }

        if (accountingCategory.Contains("費", StringComparison.Ordinal))
        {
            return "2";
        }

        return "1";
    }

    private static string ToRocYear(DateOnly date)
    {
        return (date.Year - 1911).ToString("000", CultureInfo.InvariantCulture);
    }

    private static string ToRocYearMonth(DateOnly date)
    {
        return $"{ToRocYear(date)}{date.Month:00}";
    }

    private static string ToRocDate(DateOnly date)
    {
        return $"{ToRocYear(date)}{date.Month:00}{date.Day:00}";
    }
}

public sealed record FolderBExportedFileResult(
    string SourceFileName,
    string OutputFilePath,
    int RecordCount);

public sealed record FolderBExportResult(IReadOnlyList<FolderBExportedFileResult> Files)
{
    public int ExportedFileCount => Files.Count;

    public int ExportedRecordCount => Files.Sum(file => file.RecordCount);
}