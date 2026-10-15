using System.Globalization;
using ClosedXML.Excel;

namespace InvoiceIntelligence.Core.Processor;

internal sealed class PurchaseVoucherWorkbookWriter
{
    private static readonly string[] Headers =
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

    public void Write(string outputPath, IReadOnlyList<InvoiceDecisionResult> items)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("進項憑證明細檔(表四)");

        WriteHeaders(worksheet, Headers);

        for (var index = 0; index < items.Count; index++)
        {
            var rowNumber = index + 2;
            var sequenceNumber = index % 100;
            var item = items[index];
            var record = item.Record;
            var decision = item.Decision;

            worksheet.Cell(rowNumber, 1).Value = FolderBExportValueResolver.ToRocYearMonth(record.InvoiceDate);
            worksheet.Cell(rowNumber, 2).Value = record.TaxId;
            worksheet.Cell(rowNumber, 3).Value = FolderBExportValueResolver.ToRocYear(record.InvoiceDate);
            worksheet.Cell(rowNumber, 4).Value = record.InvoiceDate.Month.ToString("00", CultureInfo.InvariantCulture);
            worksheet.Cell(rowNumber, 5).Value = record.InvoiceDate.Month.ToString("00", CultureInfo.InvariantCulture);
            worksheet.Cell(rowNumber, 6).Value = "1";
            worksheet.Cell(rowNumber, 7).Value = "0001";
            worksheet.Cell(rowNumber, 8).Value = FolderBExportValueResolver.ResolveDeductibleFlag(decision.DeductibleStatus);
            worksheet.Cell(rowNumber, 9).Value = sequenceNumber;
            worksheet.Cell(rowNumber, 10).Value = FolderBExportValueResolver.ResolveVoucherCategory(decision.AccountingCategory);
            worksheet.Cell(rowNumber, 11).Value = record.NetAmount;
            worksheet.Cell(rowNumber, 12).Value = FolderBExportValueResolver.ResolveTaxType(record);
            worksheet.Cell(rowNumber, 13).Value = record.TaxAmount;
            worksheet.Cell(rowNumber, 14).Value = FolderBExportValueResolver.ToRocDate(record.InvoiceDate);
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
}
