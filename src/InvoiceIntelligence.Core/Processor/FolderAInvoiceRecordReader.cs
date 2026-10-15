using System.Globalization;
using ClosedXML.Excel;

namespace InvoiceIntelligence.Core.Processor;

public sealed class FolderAInvoiceRecordReader
{
    private static readonly string[] LegacyRequiredHeaders =
    [
        "統編",
        "公司名稱",
        "發票號碼",
        "發票日期",
        "廠商統編",
        "品名",
        "未稅金額",
        "稅額",
        "含稅金額"
    ];

    private static readonly string[] OfficialSummaryRequiredHeaders =
    [
        "發票號碼",
        "發票日期",
        "買方統一編號",
        "賣方統一編號"
    ];

    private static readonly string[] OfficialDetailRequiredHeaders =
    [
        "發票號碼",
        "品名"
    ];

    private const string OfficialFallbackItemName = "官方下載資料";

    public IReadOnlyList<InvoiceAnalysisRecord> ReadFromFolder(
        string folderAPath,
        string invoiceFileSuffix = "invoice",
        string allowanceFileSuffix = "allowance")
    {
        if (string.IsNullOrWhiteSpace(folderAPath))
        {
            throw new ArgumentException("資料夾 A 路徑不可為空白。", nameof(folderAPath));
        }

        if (!Directory.Exists(folderAPath))
        {
            return [];
        }

        var candidateFiles = Directory
            .EnumerateFiles(folderAPath, "*.xlsx", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var invoiceFiles = candidateFiles
            .Where(path => FolderAFileCategoryHelper.IsPreferredSourceFile(path, invoiceFileSuffix, allowanceFileSuffix))
            .ToArray();

        if (invoiceFiles.Length == 0)
        {
            invoiceFiles = candidateFiles;
        }

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
        var firstWorksheet = workbook.Worksheets.FirstOrDefault();
        if (firstWorksheet is null)
        {
            throw new InvalidDataException($"發票活頁簿不包含任何工作表：{filePath}");
        }

        var firstHeaders = BuildHeaderIndex(firstWorksheet);
        if (ContainsAllHeaders(firstHeaders, LegacyRequiredHeaders))
        {
            return ReadLegacyRecords(firstWorksheet, filePath, firstHeaders);
        }

        var summaryWorksheet = workbook.Worksheets.FirstOrDefault(worksheet =>
            ContainsAllHeaders(BuildHeaderIndex(worksheet), OfficialSummaryRequiredHeaders));
        if (summaryWorksheet is not null)
        {
            return ReadOfficialRecords(workbook, summaryWorksheet, filePath);
        }

        throw new InvalidDataException($"發票工作表格式不支援：{Path.GetFileName(filePath)}");
    }

    private static IReadOnlyList<InvoiceAnalysisRecord> ReadLegacyRecords(
        IXLWorksheet worksheet,
        string filePath,
        IReadOnlyDictionary<string, int> headers)
    {
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

    private static IReadOnlyList<InvoiceAnalysisRecord> ReadOfficialRecords(
        XLWorkbook workbook,
        IXLWorksheet summaryWorksheet,
        string filePath)
    {
        var headers = BuildHeaderIndex(summaryWorksheet);
        var invoiceNoColumn = GetRequiredColumn(headers, "發票號碼");
        var invoiceDateColumn = GetRequiredColumn(headers, "發票日期");
        var buyerTaxIdColumn = GetRequiredColumn(headers, "買方統一編號");
        var sellerTaxIdColumn = GetRequiredColumn(headers, "賣方統一編號");

        var buyerNameColumn = TryGetColumn(headers, "買方名稱");
        var sellerNameColumn = TryGetColumn(headers, "賣方名稱");
        var taxableAmountColumn = TryGetColumn(headers, "應稅銷售額");
        var salesAmountColumn = TryGetColumn(headers, "銷售額合計");
        var taxAmountColumn = TryGetColumn(headers, "營業稅");
        var totalAmountColumn = TryGetColumn(headers, "總計");

        var detailItemNamesByInvoice = BuildOfficialDetailItemNames(workbook);

        var records = new List<InvoiceAnalysisRecord>();
        var lastRow = summaryWorksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var invoiceNumber = summaryWorksheet.Cell(rowNumber, invoiceNoColumn).GetString().Trim();
            var buyerTaxId = summaryWorksheet.Cell(rowNumber, buyerTaxIdColumn).GetString().Trim();
            var sellerTaxId = summaryWorksheet.Cell(rowNumber, sellerTaxIdColumn).GetString().Trim();

            if (invoiceNumber.Length == 0
                && buyerTaxId.Length == 0
                && sellerTaxId.Length == 0)
            {
                continue;
            }

            var invoiceDateRaw = summaryWorksheet.Cell(rowNumber, invoiceDateColumn).GetString().Trim();
            if (!TryParseDate(invoiceDateRaw, out var invoiceDate))
            {
                throw new InvalidDataException($"{Path.GetFileName(filePath)} row {rowNumber}: 發票日期格式無效。");
            }

            var buyerName = ReadString(summaryWorksheet, rowNumber, buyerNameColumn);
            var sellerName = ReadString(summaryWorksheet, rowNumber, sellerNameColumn);

            var taxId = buyerTaxId.Length > 0 ? buyerTaxId : sellerTaxId;
            var vendorTaxId = buyerTaxId.Length > 0 ? sellerTaxId : buyerTaxId;
            var companyName = buyerName.Length > 0 ? buyerName : sellerName;

            var netAmount = ReadFirstDecimalOrZero(summaryWorksheet, rowNumber, taxableAmountColumn, salesAmountColumn, totalAmountColumn);
            var taxAmount = ReadDecimalOrZero(summaryWorksheet, rowNumber, taxAmountColumn);
            var totalAmount = ReadFirstDecimalOrZero(summaryWorksheet, rowNumber, totalAmountColumn, salesAmountColumn, taxableAmountColumn);

            if (totalAmount == 0m && netAmount != 0m)
            {
                totalAmount = netAmount + taxAmount;
            }

            if (netAmount == 0m && totalAmount != 0m)
            {
                netAmount = Math.Max(0m, totalAmount - taxAmount);
            }

            var itemName = ResolveOfficialItemName(detailItemNamesByInvoice, invoiceNumber);

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

    private static Dictionary<string, List<string>> BuildOfficialDetailItemNames(XLWorkbook workbook)
    {
        var detailWorksheet = workbook.Worksheets.FirstOrDefault(worksheet =>
            ContainsAllHeaders(BuildHeaderIndex(worksheet), OfficialDetailRequiredHeaders));
        if (detailWorksheet is null)
        {
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }

        var headers = BuildHeaderIndex(detailWorksheet);
        var invoiceNoColumn = GetRequiredColumn(headers, "發票號碼");
        var itemNameColumn = GetRequiredColumn(headers, "品名");

        var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var lastRow = detailWorksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var invoiceNumber = detailWorksheet.Cell(rowNumber, invoiceNoColumn).GetString().Trim();
            if (invoiceNumber.Length == 0)
            {
                continue;
            }

            var itemName = detailWorksheet.Cell(rowNumber, itemNameColumn).GetString().Trim();
            if (itemName.Length == 0)
            {
                itemName = OfficialFallbackItemName;
            }

            if (!map.TryGetValue(invoiceNumber, out var itemNames))
            {
                itemNames = [];
                map[invoiceNumber] = itemNames;
            }

            if (!itemNames.Contains(itemName, StringComparer.Ordinal))
            {
                itemNames.Add(itemName);
            }
        }

        return map;
    }

    private static string ResolveOfficialItemName(
        IReadOnlyDictionary<string, List<string>> detailItemNamesByInvoice,
        string invoiceNumber)
    {
        if (!detailItemNamesByInvoice.TryGetValue(invoiceNumber, out var itemNames)
            || itemNames.Count == 0)
        {
            return OfficialFallbackItemName;
        }

        return string.Join('、', itemNames);
    }

    private static bool ContainsAllHeaders(IReadOnlyDictionary<string, int> headers, IReadOnlyCollection<string> requiredHeaders)
    {
        foreach (var requiredHeader in requiredHeaders)
        {
            if (!headers.ContainsKey(requiredHeader))
            {
                return false;
            }
        }

        return true;
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

    private static int TryGetColumn(IReadOnlyDictionary<string, int> headers, string name)
    {
        return headers.TryGetValue(name, out var column) ? column : 0;
    }

    private static string ReadString(IXLWorksheet worksheet, int rowNumber, int columnNumber)
    {
        if (columnNumber <= 0)
        {
            return string.Empty;
        }

        return worksheet.Cell(rowNumber, columnNumber).GetString().Trim();
    }

    private static decimal ReadFirstDecimalOrZero(IXLWorksheet worksheet, int rowNumber, params int[] columnCandidates)
    {
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

    private static decimal ReadDecimalOrZero(IXLWorksheet worksheet, int rowNumber, int columnNumber)
    {
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