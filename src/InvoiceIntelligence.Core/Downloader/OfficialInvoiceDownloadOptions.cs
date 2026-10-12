namespace InvoiceIntelligence.Core.Downloader;

public sealed record OfficialInvoiceDownloadOptions(
    int PageSize,
    int MaxRetryAttempts,
    TimeSpan RetryBaseDelay,
    int MaxPagesPerDirection)
{
    public static OfficialInvoiceDownloadOptions Default { get; } = new(
        PageSize: 200,
        MaxRetryAttempts: 4,
        RetryBaseDelay: TimeSpan.FromSeconds(1),
        MaxPagesPerDirection: 500);
}