using System.Globalization;
using ClosedXML.Excel;

namespace InvoiceIntelligence.Core.Processor;

internal sealed class SalesDetailWorkbookWriter
{
    private static readonly string[] Headers =
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

    public void Write(string outputPath, IReadOnlyList<InvoiceDecisionResult> items)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("銷項發票明細檔(表三)");

        WriteHeaders(worksheet, Headers);

        for (var index = 0; index < items.Count; index++)
        {
            var rowNumber = index + 2;
            var item = items[index];
            var record = item.Record;
            var decision = item.Decision;

            var taxType = FolderBExportValueResolver.ResolveTaxType(record);
            worksheet.Cell(rowNumber, 1).Value = FolderBExportValueResolver.ToRocYearMonth(record.InvoiceDate);
            worksheet.Cell(rowNumber, 2).Value = record.TaxId;
            worksheet.Cell(rowNumber, 3).Value = FolderBExportValueResolver.ToRocYearMonth(record.InvoiceDate);
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

    private static void WriteHeaders(IXLWorksheet worksheet, IReadOnlyList<string> headers)
    {
        for (var index = 0; index < headers.Count; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
        }
    }
}
