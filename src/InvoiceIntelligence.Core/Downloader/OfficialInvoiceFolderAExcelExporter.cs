using System.Globalization;
using ClosedXML.Excel;

namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Writes official downloader output as a two-sheet workbook aligned with MOF export shape.
/// </summary>
public sealed class OfficialInvoiceFolderAExcelExporter : IOfficialInvoiceExporter
{
    private static readonly string[] SummaryHeaders =
    [
        "發票號碼",
        "買受人註記",
        "格式代號",
        "發票狀態",
        "發票日期",
        "買方統一編號",
        "買方名稱",
        "賣方統一編號",
        "賣方名稱",
        "寄送日期",
        "銷售額合計",
        "應稅銷售額",
        "零稅銷售額",
        "免稅銷售額",
        "營業稅",
        "總計",
        "課稅別",
        "匯率",
        "載具類別編號",
        "載具號碼1",
        "載具號碼2",
        "總備註",
        "開立確認時間",
        "最後異動時間",
        "MIG訊息類別",
        "傳送方統編",
        "傳送方名稱"
    ];

    private static readonly string[] DetailHeaders =
    [
        "發票號碼",
        "發票日期",
        "序號",
        "品名",
        "數量",
        "單位",
        "單價",
        "金額",
        "單一欄位備註",
        "相關號碼"
    ];

    private static readonly string[] AllowanceHeaders =
    [
        "折讓單號碼",
        "格式代號",
        "折讓單狀態",
        "折讓單類別",
        "發票號碼",
        "發票日期",
        "買方統一編號",
        "買方名稱",
        "賣方統一編號",
        "賣方名稱",
        "寄送日期",
        "品項名稱",
        "品項折讓金額(不含稅)",
        "品項折讓稅額",
        "折讓金額(不含稅)",
        "折讓稅額",
        "註記欄(不轉入進銷項媒體申報檔)",
        "折讓單日期",
        "最後異動時間",
        "MIG訊息類別",
        "傳送方統編",
        "傳送方名稱"
    ];

    private static readonly string[] AllowanceNumberFieldAliases = ["折讓單號碼", "allowanceNo", "allowanceNumber"];
    private static readonly string[] AllowanceFormatCodeFieldAliases = ["格式代號", "formatCode", "invoiceType"];
    private static readonly string[] AllowanceStatusFieldAliases = ["折讓單狀態", "allowanceStatus", "status"];
    private static readonly string[] AllowanceCategoryFieldAliases = ["折讓單類別", "allowanceType", "category", "scope"];
    private static readonly string[] InvoiceNumberFieldAliases = ["發票號碼", "invoiceNo", "invoiceNumber", "invoiceNum"];
    private static readonly string[] InvoiceDateFieldAliases = ["發票日期", "invoiceDate"];
    private static readonly string[] BuyerTaxIdFieldAliases = ["買方統一編號", "buyerTaxId", "buyerBan", "buyerIdentifier"];
    private static readonly string[] BuyerNameFieldAliases = ["買方名稱", "buyerName"];
    private static readonly string[] SellerTaxIdFieldAliases = ["賣方統一編號", "sellerTaxId", "sellerBan", "sellerIdentifier"];
    private static readonly string[] SellerNameFieldAliases = ["賣方名稱", "sellerName"];
    private static readonly string[] SendDateFieldAliases = ["寄送日期", "sendDate"];
    private static readonly string[] ItemNameFieldAliases = ["品項名稱", "itemName", "description", "品名"];
    private static readonly string[] ItemNetAmountFieldAliases = ["品項折讓金額(不含稅)", "itemNetAmount", "itemAllowanceAmount"];
    private static readonly string[] ItemTaxAmountFieldAliases = ["品項折讓稅額", "itemTaxAmount"];
    private static readonly string[] NetAmountFieldAliases = ["折讓金額(不含稅)", "allowanceAmount", "allowanceNetAmount", "netAmount"];
    private static readonly string[] TaxAmountFieldAliases = ["折讓稅額", "allowanceTaxAmount", "taxAmount"];
    private static readonly string[] RemarkFieldAliases = ["註記欄(不轉入進銷項媒體申報檔)", "remark", "memo", "note"];
    private static readonly string[] AllowanceDateFieldAliases = ["折讓單日期", "allowanceDate"];
    private static readonly string[] LastUpdateTimeFieldAliases = ["最後異動時間", "lastUpdatedTime", "lastUpdateTime", "updateTime"];
    private static readonly string[] MigMessageTypeFieldAliases = ["MIG訊息類別", "migType", "migMessageType"];
    private static readonly string[] SenderTaxIdFieldAliases = ["傳送方統編", "senderTaxId", "senderBan"];
    private static readonly string[] SenderNameFieldAliases = ["傳送方名稱", "senderName"];

    public Task ExportAsync(
        IReadOnlyList<OfficialInvoiceRecord> invoices,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoices);

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Excel 輸出路徑不可為空白。", nameof(outputPath));
        }

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var workbook = new XLWorkbook();
        var summarySheet = workbook.Worksheets.Add("btb411w_xls1");
        var detailSheet = workbook.Worksheets.Add("btb411w_xls2");

        WriteHeaders(summarySheet, SummaryHeaders);
        WriteHeaders(detailSheet, DetailHeaders);

        var orderedInvoices = invoices
            .OrderBy(invoice => invoice.InvoiceDate)
            .ThenBy(invoice => invoice.InvoiceNumber, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        for (var index = 0; index < orderedInvoices.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rowNumber = index + 2;
            var sequenceNumber = (index + 1).ToString("000", CultureInfo.InvariantCulture);
            var invoice = orderedInvoices[index];
            var scopeLabel = BuildScopeLabel(invoice);

            var buyerTaxId = ResolveBuyerTaxId(invoice);
            var sellerTaxId = ResolveSellerTaxId(invoice);

            summarySheet.Cell(rowNumber, 1).Value = invoice.InvoiceNumber;
            summarySheet.Cell(rowNumber, 2).Value = scopeLabel;
            summarySheet.Cell(rowNumber, 3).Value = invoice.DocumentType == InvoiceDocumentType.Allowance ? "35" : "32";
            summarySheet.Cell(rowNumber, 4).Value = "有效";
            summarySheet.Cell(rowNumber, 5).Value = invoice.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            summarySheet.Cell(rowNumber, 6).Value = buyerTaxId;
            summarySheet.Cell(rowNumber, 7).Value = string.Empty;
            summarySheet.Cell(rowNumber, 8).Value = sellerTaxId;
            summarySheet.Cell(rowNumber, 9).Value = string.Empty;
            summarySheet.Cell(rowNumber, 10).Value = invoice.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            summarySheet.Cell(rowNumber, 11).Value = invoice.NetAmount;
            summarySheet.Cell(rowNumber, 12).Value = invoice.NetAmount;
            summarySheet.Cell(rowNumber, 13).Value = 0m;
            summarySheet.Cell(rowNumber, 14).Value = 0m;
            summarySheet.Cell(rowNumber, 15).Value = invoice.TaxAmount;
            summarySheet.Cell(rowNumber, 16).Value = invoice.TotalAmount;
            summarySheet.Cell(rowNumber, 17).Value = "應稅";
            summarySheet.Cell(rowNumber, 18).Value = 1m;
            summarySheet.Cell(rowNumber, 19).Value = string.Empty;
            summarySheet.Cell(rowNumber, 20).Value = string.Empty;
            summarySheet.Cell(rowNumber, 21).Value = string.Empty;
            summarySheet.Cell(rowNumber, 22).Value = "來源: 官方統計查詢";
            summarySheet.Cell(rowNumber, 23).Value = invoice.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            summarySheet.Cell(rowNumber, 24).Value = invoice.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            summarySheet.Cell(rowNumber, 25).Value = "STAT";
            summarySheet.Cell(rowNumber, 26).Value = sellerTaxId;
            summarySheet.Cell(rowNumber, 27).Value = string.Empty;

            detailSheet.Cell(rowNumber, 1).Value = invoice.InvoiceNumber;
            detailSheet.Cell(rowNumber, 2).Value = invoice.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            detailSheet.Cell(rowNumber, 3).Value = sequenceNumber;
            detailSheet.Cell(rowNumber, 4).Value = scopeLabel;
            detailSheet.Cell(rowNumber, 5).Value = 1m;
            detailSheet.Cell(rowNumber, 6).Value = "式";
            detailSheet.Cell(rowNumber, 7).Value = invoice.TotalAmount;
            detailSheet.Cell(rowNumber, 8).Value = invoice.TotalAmount;
            detailSheet.Cell(rowNumber, 9).Value = "來源: 官方統計查詢";
            detailSheet.Cell(rowNumber, 10).Value = string.Empty;
        }

        var allowanceRows = new List<(OfficialInvoiceRecord Invoice, IReadOnlyDictionary<string, string> Raw1Item)>();
        foreach (var invoice in orderedInvoices)
        {
            if (invoice.DocumentType != InvoiceDocumentType.Allowance
                || invoice.Raw1Items is null
                || invoice.Raw1Items.Count == 0)
            {
                continue;
            }

            foreach (var raw1Item in invoice.Raw1Items)
            {
                allowanceRows.Add((invoice, raw1Item));
            }
        }

        if (allowanceRows.Count > 0)
        {
            var allowanceSheet = workbook.Worksheets.Add("allowance");
            WriteHeaders(allowanceSheet, AllowanceHeaders);

            for (var index = 0; index < allowanceRows.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rowNumber = index + 2;
                var row = allowanceRows[index];
                var raw1Item = row.Raw1Item;
                var invoice = row.Invoice;

                allowanceSheet.Cell(rowNumber, 1).Value = ResolveRawString(raw1Item, AllowanceNumberFieldAliases)
                    ?? ResolveRawString(raw1Item, InvoiceNumberFieldAliases)
                    ?? invoice.InvoiceNumber;
                allowanceSheet.Cell(rowNumber, 2).Value = ResolveRawString(raw1Item, AllowanceFormatCodeFieldAliases) ?? "23";
                allowanceSheet.Cell(rowNumber, 3).Value = ResolveRawString(raw1Item, AllowanceStatusFieldAliases) ?? "折讓已確認";
                allowanceSheet.Cell(rowNumber, 4).Value = ResolveRawString(raw1Item, AllowanceCategoryFieldAliases)
                    ?? (invoice.Direction == InvoiceDirection.Purchase ? "進項" : "銷項");
                allowanceSheet.Cell(rowNumber, 5).Value = ResolveRawString(raw1Item, InvoiceNumberFieldAliases) ?? invoice.InvoiceNumber;
                allowanceSheet.Cell(rowNumber, 6).Value = ResolveRawString(raw1Item, InvoiceDateFieldAliases)
                    ?? invoice.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                allowanceSheet.Cell(rowNumber, 7).Value = ResolveRawString(raw1Item, BuyerTaxIdFieldAliases) ?? ResolveBuyerTaxId(invoice);
                allowanceSheet.Cell(rowNumber, 8).Value = ResolveRawString(raw1Item, BuyerNameFieldAliases) ?? string.Empty;
                allowanceSheet.Cell(rowNumber, 9).Value = ResolveRawString(raw1Item, SellerTaxIdFieldAliases) ?? ResolveSellerTaxId(invoice);
                allowanceSheet.Cell(rowNumber, 10).Value = ResolveRawString(raw1Item, SellerNameFieldAliases) ?? string.Empty;
                allowanceSheet.Cell(rowNumber, 11).Value = ResolveRawString(raw1Item, SendDateFieldAliases)
                    ?? invoice.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                allowanceSheet.Cell(rowNumber, 12).Value = ResolveRawString(raw1Item, ItemNameFieldAliases) ?? BuildScopeLabel(invoice);
                allowanceSheet.Cell(rowNumber, 13).Value = ResolveRawDecimal(raw1Item, ItemNetAmountFieldAliases) ?? invoice.NetAmount;
                allowanceSheet.Cell(rowNumber, 14).Value = ResolveRawDecimal(raw1Item, ItemTaxAmountFieldAliases) ?? invoice.TaxAmount;
                allowanceSheet.Cell(rowNumber, 15).Value = ResolveRawDecimal(raw1Item, NetAmountFieldAliases) ?? invoice.NetAmount;
                allowanceSheet.Cell(rowNumber, 16).Value = ResolveRawDecimal(raw1Item, TaxAmountFieldAliases) ?? invoice.TaxAmount;
                allowanceSheet.Cell(rowNumber, 17).Value = ResolveRawString(raw1Item, RemarkFieldAliases) ?? string.Empty;
                allowanceSheet.Cell(rowNumber, 18).Value = ResolveRawString(raw1Item, AllowanceDateFieldAliases)
                    ?? invoice.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                allowanceSheet.Cell(rowNumber, 19).Value = ResolveRawString(raw1Item, LastUpdateTimeFieldAliases)
                    ?? invoice.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                allowanceSheet.Cell(rowNumber, 20).Value = ResolveRawString(raw1Item, MigMessageTypeFieldAliases) ?? "G0401";
                allowanceSheet.Cell(rowNumber, 21).Value = ResolveRawString(raw1Item, SenderTaxIdFieldAliases) ?? ResolveSellerTaxId(invoice);
                allowanceSheet.Cell(rowNumber, 22).Value = ResolveRawString(raw1Item, SenderNameFieldAliases) ?? string.Empty;
            }

            allowanceSheet.Columns().AdjustToContents();
        }

        summarySheet.Columns().AdjustToContents();
        detailSheet.Columns().AdjustToContents();

        workbook.SaveAs(outputPath);

        return Task.CompletedTask;
    }

    private static void WriteHeaders(IXLWorksheet worksheet, IReadOnlyList<string> headers)
    {
        for (var column = 0; column < headers.Count; column++)
        {
            worksheet.Cell(1, column + 1).Value = headers[column];
        }
    }

    private static string BuildScopeLabel(OfficialInvoiceRecord invoice)
    {
        var direction = invoice.Direction == InvoiceDirection.Purchase ? "進項" : "銷項";
        var type = invoice.DocumentType == InvoiceDocumentType.Allowance ? "折讓" : "發票";
        return $"{direction}{type}";
    }

    private static string ResolveBuyerTaxId(OfficialInvoiceRecord invoice)
    {
        if (!string.IsNullOrWhiteSpace(invoice.BuyerTaxId))
        {
            return invoice.BuyerTaxId;
        }

        return invoice.Direction == InvoiceDirection.Purchase
            ? invoice.SellerTaxId
            : string.Empty;
    }

    private static string ResolveSellerTaxId(OfficialInvoiceRecord invoice)
    {
        if (!string.IsNullOrWhiteSpace(invoice.SellerTaxId))
        {
            return invoice.SellerTaxId;
        }

        return invoice.Direction == InvoiceDirection.Sales
            ? invoice.BuyerTaxId
            : string.Empty;
    }

    private static string? ResolveRawString(
        IReadOnlyDictionary<string, string> raw1Item,
        params string[] fieldNames)
    {
        foreach (var fieldName in fieldNames)
        {
            if (!raw1Item.TryGetValue(fieldName, out var rawValue)
                || string.IsNullOrWhiteSpace(rawValue))
            {
                continue;
            }

            return rawValue.Trim();
        }

        return null;
    }

    private static decimal? ResolveRawDecimal(
        IReadOnlyDictionary<string, string> raw1Item,
        params string[] fieldNames)
    {
        var rawValue = ResolveRawString(raw1Item, fieldNames);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        if (decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedValue))
        {
            return parsedValue;
        }

        if (decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.CurrentCulture, out parsedValue))
        {
            return parsedValue;
        }

        return null;
    }
}