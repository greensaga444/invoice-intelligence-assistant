using System.Globalization;
using ClosedXML.Excel;

namespace InvoiceIntelligence.Core.Common;

public static class ExcelSchemaHelper
{
    public static Dictionary<string, int> ReadHeaderIndex(IXLWorksheet worksheet, bool normalize = false)
    {
        ArgumentNullException.ThrowIfNull(worksheet);

        var headerRow = worksheet.Row(1);
        var lastColumn = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var column = 1; column <= lastColumn; column++)
        {
            var header = headerRow.Cell(column).GetString().Trim();
            if (header.Length == 0)
            {
                continue;
            }

            headers[normalize ? NormalizeHeader(header) : header] = column;
        }

        return headers;
    }

    public static int GetRequiredColumn(
        IReadOnlyDictionary<string, int> headers,
        string name,
        bool normalize = false)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var key = normalize ? NormalizeHeader(name) : name;
        if (!headers.TryGetValue(key, out var column))
        {
            throw new InvalidDataException($"發票工作表缺少必要欄位：{name}");
        }

        return column;
    }

    public static int GetRequiredColumn(
        IReadOnlyDictionary<string, int> headers,
        IEnumerable<string> aliases,
        string requiredFieldName,
        bool normalize = false)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(aliases);

        foreach (var alias in aliases)
        {
            var key = normalize ? NormalizeHeader(alias) : alias;
            if (headers.TryGetValue(key, out var column))
            {
                return column;
            }
        }

        throw new InvalidDataException($"缺少必要欄位：{requiredFieldName}。");
    }

    public static int TryGetColumn(
        IReadOnlyDictionary<string, int> headers,
        string name,
        bool normalize = false)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var key = normalize ? NormalizeHeader(name) : name;
        return headers.TryGetValue(key, out var column) ? column : 0;
    }

    public static string ReadString(IXLWorksheet worksheet, int rowNumber, int columnNumber)
    {
        ArgumentNullException.ThrowIfNull(worksheet);

        if (columnNumber <= 0)
        {
            return string.Empty;
        }

        return worksheet.Cell(rowNumber, columnNumber).GetString().Trim();
    }

    public static decimal ReadDecimal(IXLCell cell)
    {
        ArgumentNullException.ThrowIfNull(cell);

        if (cell.TryGetValue<decimal>(out var decimalValue))
        {
            return decimalValue;
        }

        var rawValue = cell.GetString().Trim();
        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out decimalValue))
        {
            return decimalValue;
        }

        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.CurrentCulture, out decimalValue))
        {
            return decimalValue;
        }

        throw new InvalidDataException($"數值格式無效：{rawValue}");
    }

    public static decimal ReadDecimalOrZero(IXLWorksheet worksheet, int rowNumber, int columnNumber)
    {
        ArgumentNullException.ThrowIfNull(worksheet);

        if (columnNumber <= 0)
        {
            return 0m;
        }

        var cell = worksheet.Cell(rowNumber, columnNumber);
        if (cell.IsEmpty() && cell.GetString().Trim().Length == 0)
        {
            return 0m;
        }

        return ReadDecimal(cell);
    }

    public static decimal ReadFirstDecimalOrZero(IXLWorksheet worksheet, int rowNumber, params int[] columnCandidates)
    {
        ArgumentNullException.ThrowIfNull(worksheet);
        ArgumentNullException.ThrowIfNull(columnCandidates);

        foreach (var columnNumber in columnCandidates)
        {
            if (columnNumber <= 0)
            {
                continue;
            }

            var cell = worksheet.Cell(rowNumber, columnNumber);
            if (cell.IsEmpty() && cell.GetString().Trim().Length == 0)
            {
                continue;
            }

            return ReadDecimal(cell);
        }

        return 0m;
    }

    public static string NormalizeHeader(string header)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(header);

        return header.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
    }
}
