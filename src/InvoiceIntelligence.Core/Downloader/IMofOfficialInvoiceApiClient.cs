namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Provides paged access to MOF official merchant invoice APIs.
/// </summary>
public interface IMofOfficialInvoiceApiClient
{
    /// <summary>
    /// Queries one logical page of official invoice/statistical data.
    /// </summary>
    Task<OfficialInvoicePageResult> QueryInvoicesAsync(
        OfficialInvoicePageRequest request,
        CancellationToken cancellationToken = default);
}