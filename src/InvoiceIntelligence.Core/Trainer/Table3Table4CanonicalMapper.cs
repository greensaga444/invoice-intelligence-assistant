using System.Globalization;
using ClosedXML.Excel;

namespace InvoiceIntelligence.Core.Trainer;

internal sealed class Table3Table4CanonicalMapper
{
    private static readonly char[] WhitespaceSeparators = [' ', '\t', '\r', '\n'];

    private static readonly IReadOnlyDictionary<TrainingCanonicalField, string[]> HeaderAliases =
        new Dictionary<TrainingCanonicalField, string[]>
        {
            [TrainingCanonicalField.CompanyName] = ["公司名稱", "公司", "客戶名稱", "營業人名稱"],
            [TrainingCanonicalField.InvoiceDate] = ["發票日期", "日期", "憑證日期"],
            [TrainingCanonicalField.ItemName] = ["品名", "品項", "摘要", "內容"],
            [TrainingCanonicalField.DeductibleStatus] = ["可抵稅", "AI_可抵稅", "是否可抵稅"],
            [TrainingCanonicalField.AccountingCategory] = ["建議科目", "AI_建議科目", "科目"],
            [TrainingCanonicalField.DeductionReason] = ["判定原因", "AI_判定原因", "原因"]
        };

    private static readonly IReadOnlyDictionary<string, string> DeductibleStatusVocabulary =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["可抵"] = "可抵稅",
            ["可扣抵"] = "可抵稅",
            ["可抵稅"] = "可抵稅",
            ["不可抵"] = "不可抵稅",
            ["不可扣抵"] = "不可抵稅",
            ["不可抵稅"] = "不可抵稅",
            ["不判定"] = "不判定",
            ["免判定"] = "不判定"
        };

    public (IXLWorksheet Worksheet, Dictionary<TrainingCanonicalField, int> Columns)? ResolveWorksheetAndColumns(XLWorkbook workbook)
    {
        ArgumentNullException.ThrowIfNull(workbook);

        foreach (var worksheet in workbook.Worksheets)
        {
            var map = BuildColumnIndexMap(worksheet);
            if (map is not null)
            {
                return (worksheet, map);
            }
        }

        return null;
    }

    public CanonicalTrainingRowParseResult ParseRow(
        IXLWorksheet worksheet,
        int rowNumber,
        IReadOnlyDictionary<TrainingCanonicalField, int> columnIndexes)
    {
        ArgumentNullException.ThrowIfNull(worksheet);
        ArgumentNullException.ThrowIfNull(columnIndexes);

        var companyName = GetNormalizedCellValue(worksheet, rowNumber, columnIndexes[TrainingCanonicalField.CompanyName]);
        var invoiceDateText = GetNormalizedCellValue(worksheet, rowNumber, columnIndexes[TrainingCanonicalField.InvoiceDate]);
        var itemName = GetNormalizedCellValue(worksheet, rowNumber, columnIndexes[TrainingCanonicalField.ItemName]);
        var deductibleStatusRaw = GetNormalizedCellValue(worksheet, rowNumber, columnIndexes[TrainingCanonicalField.DeductibleStatus]);
        var accountingCategory = GetNormalizedCellValue(worksheet, rowNumber, columnIndexes[TrainingCanonicalField.AccountingCategory]);
        var deductionReason = GetNormalizedCellValue(worksheet, rowNumber, columnIndexes[TrainingCanonicalField.DeductionReason]);

        var values = new[] { companyName, invoiceDateText, itemName, deductibleStatusRaw, accountingCategory, deductionReason };
        if (values.All(string.IsNullOrWhiteSpace))
        {
            return new CanonicalTrainingRowParseResult(Row: null, IsEmptyRow: true, Issues: []);
        }

        var issues = new List<string>();
        if (string.IsNullOrWhiteSpace(companyName))
        {
            issues.Add("公司名稱為空");
        }

        if (string.IsNullOrWhiteSpace(itemName))
        {
            issues.Add("品名為空");
        }

        if (string.IsNullOrWhiteSpace(accountingCategory))
        {
            issues.Add("建議科目為空");
        }

        if (string.IsNullOrWhiteSpace(deductionReason))
        {
            issues.Add("判定原因為空");
        }

        string deductibleStatus = string.Empty;
        if (string.IsNullOrWhiteSpace(deductibleStatusRaw))
        {
            issues.Add("可抵稅標籤為空");
        }
        else if (!TryCanonicalizeDeductibleStatus(deductibleStatusRaw, out deductibleStatus))
        {
            issues.Add("可抵稅標籤不在治理字典中");
        }

        if (!TryParseDate(invoiceDateText, out var invoiceDate))
        {
            issues.Add("發票日期格式無法解析");
        }

        if (issues.Count > 0)
        {
            return new CanonicalTrainingRowParseResult(Row: null, IsEmptyRow: false, Issues: issues);
        }

        return new CanonicalTrainingRowParseResult(
            new CanonicalTrainingRow(
                CompanyName: companyName,
                InvoiceDate: invoiceDate,
                ItemName: itemName,
                DeductibleStatus: deductibleStatus,
                AccountingCategory: accountingCategory,
                DeductionReason: deductionReason),
            IsEmptyRow: false,
            Issues: []);
    }

    private static Dictionary<TrainingCanonicalField, int>? BuildColumnIndexMap(IXLWorksheet worksheet)
    {
        var headerRow = worksheet.Row(1);
        var lastColumnNumber = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
        if (lastColumnNumber == 0)
        {
            return null;
        }

        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var column = 1; column <= lastColumnNumber; column++)
        {
            var normalizedHeader = NormalizeHeader(headerRow.Cell(column).GetString());
            if (string.IsNullOrWhiteSpace(normalizedHeader) || headers.ContainsKey(normalizedHeader))
            {
                continue;
            }

            headers[normalizedHeader] = column;
        }

        var result = new Dictionary<TrainingCanonicalField, int>();
        foreach (var pair in HeaderAliases)
        {
            var matchedColumn = pair.Value
                .Select(alias => headers.TryGetValue(NormalizeHeader(alias), out var column) ? column : 0)
                .FirstOrDefault(column => column > 0);

            if (matchedColumn == 0)
            {
                return null;
            }

            result[pair.Key] = matchedColumn;
        }

        return result;
    }

    private static bool TryCanonicalizeDeductibleStatus(string input, out string canonical)
    {
        var normalized = NormalizeText(input);
        if (DeductibleStatusVocabulary.TryGetValue(normalized, out canonical!))
        {
            return true;
        }

        canonical = string.Empty;
        return false;
    }

    private static bool TryParseDate(string input, out DateOnly result)
    {
        var text = NormalizeText(input);
        if (DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
        {
            return true;
        }

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            result = DateOnly.FromDateTime(dateTime);
            return true;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var serialDate))
        {
            try
            {
                result = DateOnly.FromDateTime(DateTime.FromOADate(serialDate));
                return true;
            }
            catch (ArgumentException)
            {
                // Fall through to return false.
            }
        }

        result = default;
        return false;
    }

    private static string GetNormalizedCellValue(IXLWorksheet worksheet, int rowNumber, int columnNumber)
    {
        return NormalizeText(worksheet.Cell(rowNumber, columnNumber).GetValue<string>());
    }

    private static string NormalizeHeader(string value)
    {
        return NormalizeText(value);
    }

    private static string NormalizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(' ', value.Trim().Split(WhitespaceSeparators, StringSplitOptions.RemoveEmptyEntries));
    }
}

internal enum TrainingCanonicalField
{
    CompanyName,
    InvoiceDate,
    ItemName,
    DeductibleStatus,
    AccountingCategory,
    DeductionReason
}

internal sealed record CanonicalTrainingRow(
    string CompanyName,
    DateOnly InvoiceDate,
    string ItemName,
    string DeductibleStatus,
    string AccountingCategory,
    string DeductionReason);

internal sealed record CanonicalTrainingRowParseResult(
    CanonicalTrainingRow? Row,
    bool IsEmptyRow,
    IReadOnlyList<string> Issues);