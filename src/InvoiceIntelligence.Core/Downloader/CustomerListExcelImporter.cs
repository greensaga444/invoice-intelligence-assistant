using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace InvoiceIntelligence.Core.Downloader;

public sealed class CustomerListExcelImporter : ICustomerListImporter
{
    private const string TaxIdField = "Tax ID";
    private const string CompanyNameField = "Company Name";
    private const string DateRangeField = "Date Range";

    private static readonly Regex TaxIdRegex = new(
        "^[0-9]{8}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex DateRegex = new(
        @"\d{4}[-/]\d{1,2}[-/]\d{1,2}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly string[] TaxIdAliases = ["Tax ID", "TaxId", "統編"];
    private static readonly string[] CompanyNameAliases = ["Company Name", "CompanyName", "公司名稱"];
    private static readonly string[] DateRangeAliases = ["Date Range", "DateRange", "日期區間", "期間"];

    public CustomerImportResult Import(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("客戶清單檔案路徑不可為空白。", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("找不到客戶清單檔案。", filePath);
        }

        if (!string.Equals(Path.GetExtension(filePath), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("客戶清單必須為 .xlsx Excel 檔案。");
        }

        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet is null)
        {
            throw new InvalidDataException("客戶清單 Excel 檔案不包含任何工作表。");
        }

        var headerIndex = ReadHeaderIndex(worksheet);
        var taxIdColumn = GetRequiredColumn(headerIndex, TaxIdAliases, TaxIdField);
        var companyColumn = GetRequiredColumn(headerIndex, CompanyNameAliases, CompanyNameField);
        var dateRangeColumn = GetRequiredColumn(headerIndex, DateRangeAliases, DateRangeField);

        var records = new List<CustomerRecord>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var taxId = worksheet.Cell(rowNumber, taxIdColumn).GetString().Trim();
            var companyName = worksheet.Cell(rowNumber, companyColumn).GetString().Trim();
            var dateRangeRaw = worksheet.Cell(rowNumber, dateRangeColumn).GetString().Trim();

            if (taxId.Length == 0 && companyName.Length == 0 && dateRangeRaw.Length == 0)
            {
                continue;
            }

            ValidateTaxId(taxId, rowNumber);
            ValidateCompanyName(companyName, rowNumber);

            if (!TryParseDateRange(dateRangeRaw, out var dateFrom, out var dateTo))
            {
                throw new InvalidDataException($"第 {rowNumber} 列：日期區間需包含兩個有效日期。");
            }

            if (dateFrom > dateTo)
            {
                throw new InvalidDataException($"第 {rowNumber} 列：日期區間起日不可晚於迄日。");
            }

            records.Add(new CustomerRecord(taxId, companyName, dateFrom, dateTo));
        }

        if (records.Count == 0)
        {
            throw new InvalidDataException("客戶清單不包含任何有效資料列。");
        }

        return new CustomerImportResult(records);
    }

    private static Dictionary<string, int> ReadHeaderIndex(IXLWorksheet worksheet)
    {
        var headerRow = worksheet.Row(1);
        var lastColumn = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

        if (lastColumn == 0)
        {
            throw new InvalidDataException("客戶清單標題列為空白。");
        }

        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var column = 1; column <= lastColumn; column++)
        {
            var rawHeader = headerRow.Cell(column).GetString().Trim();
            if (rawHeader.Length == 0)
            {
                continue;
            }

            headerIndex[NormalizeHeader(rawHeader)] = column;
        }

        return headerIndex;
    }

    private static int GetRequiredColumn(
        IReadOnlyDictionary<string, int> headerIndex,
        IEnumerable<string> aliases,
        string requiredFieldName)
    {
        foreach (var alias in aliases)
        {
            if (headerIndex.TryGetValue(NormalizeHeader(alias), out var column))
            {
                return column;
            }
        }

        throw new InvalidDataException($"缺少必要欄位：{requiredFieldName}。");
    }

    private static string NormalizeHeader(string header)
    {
        return header.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
    }

    private static void ValidateTaxId(string taxId, int rowNumber)
    {
        if (taxId.Length == 0)
        {
            throw new InvalidDataException($"第 {rowNumber} 列：統編不可為空白。");
        }

        if (!TaxIdRegex.IsMatch(taxId))
        {
            throw new InvalidDataException($"第 {rowNumber} 列：統編必須為 8 位數字。");
        }
    }

    private static void ValidateCompanyName(string companyName, int rowNumber)
    {
        if (companyName.Length == 0)
        {
            throw new InvalidDataException($"第 {rowNumber} 列：公司名稱不可為空白。");
        }
    }

    private static bool TryParseDateRange(string rawValue, out DateOnly dateFrom, out DateOnly dateTo)
    {
        dateFrom = default;
        dateTo = default;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        var parts = rawValue.Split(['~', '～'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            return TryParseDate(parts[0], out dateFrom) && TryParseDate(parts[1], out dateTo);
        }

        var dateMatches = DateRegex.Matches(rawValue);
        if (dateMatches.Count >= 2)
        {
            return TryParseDate(dateMatches[0].Value, out dateFrom) && TryParseDate(dateMatches[1].Value, out dateTo);
        }

        return false;
    }

    private static bool TryParseDate(string input, out DateOnly value)
    {
        if (DateOnly.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
        {
            return true;
        }

        return DateOnly.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.None, out value);
    }
}
