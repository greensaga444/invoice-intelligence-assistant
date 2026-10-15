using ClosedXML.Excel;
using InvoiceIntelligence.Core.Common;

namespace InvoiceIntelligence.Core.Processor;

internal sealed class LegacyFolderAWorksheetRecordReader : IFolderAWorksheetRecordReader
{
    private static readonly string[] RequiredHeaders =
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

    public bool CanRead(XLWorkbook workbook, IXLWorksheet firstWorksheet)
    {
        ArgumentNullException.ThrowIfNull(firstWorksheet);

        var headers = ExcelSchemaHelper.ReadHeaderIndex(firstWorksheet);
        return RequiredHeaders.All(headers.ContainsKey);
    }

    public IReadOnlyList<InvoiceAnalysisRecord> Read(XLWorkbook workbook, string filePath)
    {
        ArgumentNullException.ThrowIfNull(workbook);

        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidDataException($"發票活頁簿不包含任何工作表：{filePath}");

        var headers = ExcelSchemaHelper.ReadHeaderIndex(worksheet);
        var invoiceNoColumn = ExcelSchemaHelper.GetRequiredColumn(headers, "發票號碼");
        var taxIdColumn = ExcelSchemaHelper.GetRequiredColumn(headers, "統編");
        var companyNameColumn = ExcelSchemaHelper.GetRequiredColumn(headers, "公司名稱");
        var invoiceDateColumn = ExcelSchemaHelper.GetRequiredColumn(headers, "發票日期");
        var vendorTaxIdColumn = ExcelSchemaHelper.GetRequiredColumn(headers, "廠商統編");
        var itemNameColumn = ExcelSchemaHelper.GetRequiredColumn(headers, "品名");
        var netAmountColumn = ExcelSchemaHelper.GetRequiredColumn(headers, "未稅金額");
        var taxAmountColumn = ExcelSchemaHelper.GetRequiredColumn(headers, "稅額");
        var totalAmountColumn = ExcelSchemaHelper.GetRequiredColumn(headers, "含稅金額");

        var records = new List<InvoiceAnalysisRecord>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var invoiceNumber = ExcelSchemaHelper.ReadString(worksheet, rowNumber, invoiceNoColumn);
            var taxId = ExcelSchemaHelper.ReadString(worksheet, rowNumber, taxIdColumn);
            var companyName = ExcelSchemaHelper.ReadString(worksheet, rowNumber, companyNameColumn);
            var vendorTaxId = ExcelSchemaHelper.ReadString(worksheet, rowNumber, vendorTaxIdColumn);
            var itemName = ExcelSchemaHelper.ReadString(worksheet, rowNumber, itemNameColumn);

            if (invoiceNumber.Length == 0
                && taxId.Length == 0
                && companyName.Length == 0
                && itemName.Length == 0)
            {
                continue;
            }

            var invoiceDateRaw = ExcelSchemaHelper.ReadString(worksheet, rowNumber, invoiceDateColumn);
            if (!DateOnlyParsingHelper.TryParseDate(invoiceDateRaw, out var invoiceDate))
            {
                throw new InvalidDataException($"{Path.GetFileName(filePath)} row {rowNumber}: 發票日期格式無效。");
            }

            var netAmount = ExcelSchemaHelper.ReadDecimal(worksheet.Cell(rowNumber, netAmountColumn));
            var taxAmount = ExcelSchemaHelper.ReadDecimal(worksheet.Cell(rowNumber, taxAmountColumn));
            var totalAmount = ExcelSchemaHelper.ReadDecimal(worksheet.Cell(rowNumber, totalAmountColumn));

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
}
