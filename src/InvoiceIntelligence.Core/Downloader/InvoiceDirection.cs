namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Invoice direction for API query and downstream processing.
/// </summary>
public enum InvoiceDirection
{
    /// <summary>
    /// Sales invoices issued by the customer.
    /// </summary>
    Sales = 1,

    /// <summary>
    /// Purchase invoices received by the customer.
    /// </summary>
    Purchase = 2
}