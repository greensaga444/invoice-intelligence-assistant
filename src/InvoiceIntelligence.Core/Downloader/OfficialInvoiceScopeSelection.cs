namespace InvoiceIntelligence.Core.Downloader;

public sealed record OfficialInvoiceScopeSelection(
    bool IncludeSalesInvoices,
    bool IncludePurchaseInvoices,
    bool IncludeSalesAllowances,
    bool IncludePurchaseAllowances)
{
    public static OfficialInvoiceScopeSelection Default { get; } = new(
        IncludeSalesInvoices: true,
        IncludePurchaseInvoices: true,
        IncludeSalesAllowances: true,
        IncludePurchaseAllowances: true);

    public bool IncludesAnyInvoice => IncludeSalesInvoices || IncludePurchaseInvoices;

    public bool IncludesAnyAllowance => IncludeSalesAllowances || IncludePurchaseAllowances;

    public bool IncludesAnyScope => IncludesAnyInvoice || IncludesAnyAllowance;

    public void Validate()
    {
        if (!IncludesAnyScope)
        {
            throw new InvalidOperationException("官方下載模式至少需啟用銷項/進項發票或銷貨/進貨折讓其中一種查詢類型。");
        }
    }
}
