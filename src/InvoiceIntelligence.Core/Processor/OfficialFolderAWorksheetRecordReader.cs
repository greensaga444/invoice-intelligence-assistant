using ClosedXML.Excel;
using InvoiceIntelligence.Core.Common;

namespace InvoiceIntelligence.Core.Processor;

internal sealed class OfficialFolderAWorksheetRecordReader : IFolderAWorksheetRecordReader
{
    private static readonly string[] SummaryRequiredHeaders =
    [
        "發票號碼",
        "發票日期",
        "買方統一編號",
        "賣方統一編號"
    ];

    private static readonly string[] DetailRequiredHeaders =
    [
        "發票號碼",
        "品名"
    ];

    private const string OfficialFallbackItemName = "官方下載資料";

    public bool CanRead(XLWorkbook workbook, IXLWorksheet firstWorksheet)
    {
        ArgumentNullException.ThrowIfNull(workbook);

        return FindSummaryWorksheet(workbook) is not null;
    }

    public IReadOnlyList<InvoiceAnalysisRecord> Read(XLWorkbook workbook, string filePath)
    {
        ArgumentNullException.ThrowIfNull(workbook);

        var summaryWorksheet = FindSummaryWorksheet(workbook)
            ?? throw new InvalidDataException($"發票工作表格式不支援：{Path.GetFileName(filePath)}");

        var headers = ExcelSchemaHelper.ReadHeaderIndex(summaryWorksheet);
        var invoiceNoColumn = ExcelSchemaHelper.GetRequiredColumn(headers, "發票號碼");
        var invoiceDateColumn = ExcelSchemaHelper.GetRequiredColumn(headers, "發票日期");
        var buyerTaxIdColumn = ExcelSchemaHelper.GetRequiredColumn(headers, "買方統一編號");
        var sellerTaxIdColumn = ExcelSchemaHelper.GetRequiredColumn(headers, "賣方統一編號");

        var buyerNameColumn = ExcelSchemaHelper.TryGetColumn(headers, "買方名稱");
        var sellerNameColumn = ExcelSchemaHelper.TryGetColumn(headers, "賣方名稱");
        var taxableAmountColumn = ExcelSchemaHelper.TryGetColumn(headers, "應稅銷售額");
        var salesAmountColumn = ExcelSchemaHelper.TryGetColumn(headers, "銷售額合計");
        var taxAmountColumn = ExcelSchemaHelper.TryGetColumn(headers, "營業稅");
        var totalAmountColumn = ExcelSchemaHelper.TryGetColumn(headers, "總計");

        var detailItemNamesByInvoice = BuildDetailItemNames(workbook);

        var records = new List<InvoiceAnalysisRecord>();
        var lastRow = summaryWorksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var invoiceNumber = ExcelSchemaHelper.ReadString(summaryWorksheet, rowNumber, invoiceNoColumn);
            var buyerTaxId = ExcelSchemaHelper.ReadString(summaryWorksheet, rowNumber, buyerTaxIdColumn);
            var sellerTaxId = ExcelSchemaHelper.ReadString(summaryWorksheet, rowNumber, sellerTaxIdColumn);

            if (invoiceNumber.Length == 0
                && buyerTaxId.Length == 0
                && sellerTaxId.Length == 0)
            {
                continue;
            }

            var invoiceDateRaw = ExcelSchemaHelper.ReadString(summaryWorksheet, rowNumber, invoiceDateColumn);
            if (!DateOnlyParsingHelper.TryParseDate(invoiceDateRaw, out var invoiceDate))
            {
                throw new InvalidDataException($"{Path.GetFileName(filePath)} row {rowNumber}: 發票日期格式無效。");
            }

            var buyerName = ExcelSchemaHelper.ReadString(summaryWorksheet, rowNumber, buyerNameColumn);
            var sellerName = ExcelSchemaHelper.ReadString(summaryWorksheet, rowNumber, sellerNameColumn);

            var taxId = buyerTaxId.Length > 0 ? buyerTaxId : sellerTaxId;
            var vendorTaxId = buyerTaxId.Length > 0 ? sellerTaxId : buyerTaxId;
            var companyName = buyerName.Length > 0 ? buyerName : sellerName;

            var netAmount = ExcelSchemaHelper.ReadFirstDecimalOrZero(summaryWorksheet, rowNumber, taxableAmountColumn, salesAmountColumn, totalAmountColumn);
            var taxAmount = ExcelSchemaHelper.ReadDecimalOrZero(summaryWorksheet, rowNumber, taxAmountColumn);
            var totalAmount = ExcelSchemaHelper.ReadFirstDecimalOrZero(summaryWorksheet, rowNumber, totalAmountColumn, salesAmountColumn, taxableAmountColumn);

            if (totalAmount == 0m && netAmount != 0m)
            {
                totalAmount = netAmount + taxAmount;
            }

            if (netAmount == 0m && totalAmount != 0m)
            {
                netAmount = Math.Max(0m, totalAmount - taxAmount);
            }

            var itemName = ResolveItemName(detailItemNamesByInvoice, invoiceNumber);

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

    private static IXLWorksheet? FindSummaryWorksheet(XLWorkbook workbook)
    {
        return workbook.Worksheets.FirstOrDefault(worksheet =>
        {
            var headers = ExcelSchemaHelper.ReadHeaderIndex(worksheet);
            return SummaryRequiredHeaders.All(headers.ContainsKey);
        });
    }

    private static Dictionary<string, List<string>> BuildDetailItemNames(XLWorkbook workbook)
    {
        var detailWorksheet = workbook.Worksheets.FirstOrDefault(worksheet =>
        {
            var headers = ExcelSchemaHelper.ReadHeaderIndex(worksheet);
            return DetailRequiredHeaders.All(headers.ContainsKey);
        });

        if (detailWorksheet is null)
        {
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }

        var headers = ExcelSchemaHelper.ReadHeaderIndex(detailWorksheet);
        var invoiceNoColumn = ExcelSchemaHelper.GetRequiredColumn(headers, "發票號碼");
        var itemNameColumn = ExcelSchemaHelper.GetRequiredColumn(headers, "品名");

        var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var lastRow = detailWorksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var invoiceNumber = ExcelSchemaHelper.ReadString(detailWorksheet, rowNumber, invoiceNoColumn);
            if (invoiceNumber.Length == 0)
            {
                continue;
            }

            var itemName = ExcelSchemaHelper.ReadString(detailWorksheet, rowNumber, itemNameColumn);
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

    private static string ResolveItemName(
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
}
