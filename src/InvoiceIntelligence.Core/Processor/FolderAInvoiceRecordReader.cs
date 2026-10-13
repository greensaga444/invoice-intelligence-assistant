using System.Globalization;
using ClosedXML.Excel;

namespace InvoiceIntelligence.Core.Processor;

public sealed class FolderAInvoiceRecordReader
{
    public IReadOnlyList<InvoiceAnalysisRecord> ReadFromFolder(string folderAPath)
    {
        if (string.IsNullOrWhiteSpace(folderAPath))
        {
            throw new ArgumentException("資料夾 A 路徑不可為空白。", nameof(folderAPath));
        }

        if (!Directory.Exists(folderAPath))
        {
            return [];
        }

        var invoiceFiles = Directory
            .EnumerateFiles(folderAPath, "*.xlsx", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var allRecords = new List<InvoiceAnalysisRecord>();

        foreach (var filePath in invoiceFiles)
        {
            allRecords.AddRange(ReadFromFile(filePath));
        }

        return allRecords;
    }

    private static IReadOnlyList<InvoiceAnalysisRecord> ReadFromFile(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet is null)
        {
            throw new InvalidDataException($"發票活頁簿不包含任何工作表：{filePath}");
        }

        var headers = BuildHeaderIndex(worksheet);

        var invoiceNoColumn = GetRequiredColumn(headers, "發票號碼");
        var taxIdColumn = GetRequiredColumn(headers, "統編");
        var companyNameColumn = GetRequiredColumn(headers, "公司名稱");
        var invoiceDateColumn = GetRequiredColumn(headers, "發票日期");
        var vendorTaxIdColumn = GetRequiredColumn(headers, "廠商統編");
        var itemNameColumn = GetRequiredColumn(headers, "品名");
        var netAmountColumn = GetRequiredColumn(headers, "未稅金額");
        var taxAmountColumn = GetRequiredColumn(headers, "稅額");
        var totalAmountColumn = GetRequiredColumn(headers, "含稅金額");

        var records = new List<InvoiceAnalysisRecord>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var invoiceNumber = worksheet.Cell(rowNumber, invoiceNoColumn).GetString().Trim();
            var taxId = worksheet.Cell(rowNumber, taxIdColumn).GetString().Trim();
            var companyName = worksheet.Cell(rowNumber, companyNameColumn).GetString().Trim();
            var vendorTaxId = worksheet.Cell(rowNumber, vendorTaxIdColumn).GetString().Trim();
            var itemName = worksheet.Cell(rowNumber, itemNameColumn).GetString().Trim();

            if (invoiceNumber.Length == 0
                && taxId.Length == 0
                && companyName.Length == 0
                && itemName.Length == 0)
            {
                continue;
            }

            var invoiceDateRaw = worksheet.Cell(rowNumber, invoiceDateColumn).GetString().Trim();
            if (!TryParseDate(invoiceDateRaw, out var invoiceDate))
            {
                throw new InvalidDataException($"{Path.GetFileName(filePath)} row {rowNumber}: 發票日期格式無效。");
            }

            var netAmount = ReadDecimal(worksheet.Cell(rowNumber, netAmountColumn));
            var taxAmount = ReadDecimal(worksheet.Cell(rowNumber, taxAmountColumn));
            var totalAmount = ReadDecimal(worksheet.Cell(rowNumber, totalAmountColumn));

            records.Add(new InvoiceAnalysisRecord(
                InvoiceNumber: invoiceNumber,
                TaxId: taxId,
                CompanyName: companyName,
                VendorTaxId: vendorTaxId,
                ItemName: itemName,
                InvoiceDate: invoiceDate,
                NetAmount: netAmount,
                TaxAmount: taxAmount,
                TotalAmount: totalAmount,
                SourceFileName: Path.GetFileName(filePath)));
        }

        return records;
    }

    private static Dictionary<string, int> BuildHeaderIndex(IXLWorksheet worksheet)
    {
        var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var headerRow = worksheet.Row(1);
        var lastColumn = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;

        for (var column = 1; column <= lastColumn; column++)
        {
            var header = headerRow.Cell(column).GetString().Trim();
            if (header.Length == 0)
            {
                continue;
            }

            headerMap[header] = column;
        }

        return headerMap;
    }

    private static int GetRequiredColumn(IReadOnlyDictionary<string, int> headers, string name)
    {
        if (!headers.TryGetValue(name, out var column))
        {
            throw new InvalidDataException($"發票工作表缺少必要欄位：{name}");
        }

        return column;
    }

    private static bool TryParseDate(string rawValue, out DateOnly invoiceDate)
    {
        if (DateOnly.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out invoiceDate))
        {
            return true;
        }

        return DateOnly.TryParse(rawValue, CultureInfo.CurrentCulture, DateTimeStyles.None, out invoiceDate);
    }

    private static decimal ReadDecimal(IXLCell cell)
    {
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
}