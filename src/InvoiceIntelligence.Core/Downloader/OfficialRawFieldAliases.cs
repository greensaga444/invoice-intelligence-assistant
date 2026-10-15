namespace InvoiceIntelligence.Core.Downloader;

internal static class OfficialRawFieldAliases
{
    public static readonly string[] InvoiceNumber = ["發票號碼", "invoiceNo", "invoiceNumber", "invoiceNum"];
    public static readonly string[] AllowanceNumber = ["折讓單號碼", "allowanceNo", "allowanceNumber"];
    public static readonly string[] InvoiceDate = ["發票日期", "invoiceDate"];
    public static readonly string[] AllowanceDate = ["折讓單日期", "allowanceDate"];
    public static readonly string[] BuyerTaxId = ["買方統一編號", "buyerTaxId", "buyerBan", "buyerIdentifier"];
    public static readonly string[] SellerTaxId = ["賣方統一編號", "sellerTaxId", "sellerBan", "sellerIdentifier"];
    public static readonly string[] NetAmount = ["折讓金額(不含稅)", "allowanceAmount", "allowanceNetAmount", "netAmount"];
    public static readonly string[] ItemNetAmount = ["品項折讓金額(不含稅)", "itemNetAmount", "itemAllowanceAmount"];
    public static readonly string[] TaxAmount = ["折讓稅額", "allowanceTaxAmount", "taxAmount"];
    public static readonly string[] ItemTaxAmount = ["品項折讓稅額", "itemTaxAmount"];
    public static readonly string[] TotalAmount = ["折讓總額", "totalAmount", "allowanceTotalAmount"];
    public static readonly string[] BuyerName = ["買方名稱", "buyerName"];
    public static readonly string[] SellerName = ["賣方名稱", "sellerName"];
    public static readonly string[] SendDate = ["寄送日期", "sendDate"];
    public static readonly string[] ItemName = ["品項名稱", "itemName", "description", "品名"];
    public static readonly string[] AllowanceFormatCode = ["格式代號", "formatCode", "invoiceType"];
    public static readonly string[] AllowanceStatus = ["折讓單狀態", "allowanceStatus", "status"];
    public static readonly string[] AllowanceCategory = ["折讓單類別", "allowanceType", "category", "scope"];
    public static readonly string[] Remark = ["註記欄(不轉入進銷項媒體申報檔)", "remark", "memo", "note"];
    public static readonly string[] LastUpdateTime = ["最後異動時間", "lastUpdatedTime", "lastUpdateTime", "updateTime"];
    public static readonly string[] MigMessageType = ["MIG訊息類別", "migType", "migMessageType"];
    public static readonly string[] SenderTaxId = ["傳送方統編", "senderTaxId", "senderBan"];
    public static readonly string[] SenderName = ["傳送方名稱", "senderName"];

    public static readonly string[] NetAmountAll = [.. NetAmount, .. ItemNetAmount];
    public static readonly string[] TaxAmountAll = [.. TaxAmount, .. ItemTaxAmount];
}
