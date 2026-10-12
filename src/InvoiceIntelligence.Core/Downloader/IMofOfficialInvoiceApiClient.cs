namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Provides paged access to MOF official merchant invoice APIs.
/// </summary>
public interface IMofOfficialInvoiceApiClient
{
    Task<OfficialInvoicePageResult> QueryInvoicesAsync(
        OfficialInvoicePageRequest request,
        CancellationToken cancellationToken = default);
}