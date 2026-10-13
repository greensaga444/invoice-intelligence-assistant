namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Tunable options controlling official API pagination and retry behavior.
/// </summary>
public sealed record OfficialInvoiceDownloadOptions(
    int PageSize,
    int MaxRetryAttempts,
    TimeSpan RetryBaseDelay,
    int MaxPagesPerDirection)
{
    /// <summary>
    /// Default values intended for stable production-like batch runs.
    /// </summary>
    public static OfficialInvoiceDownloadOptions Default { get; } = new(
        PageSize: 200,
        MaxRetryAttempts: 4,
        RetryBaseDelay: TimeSpan.FromSeconds(1),
        MaxPagesPerDirection: 500);
}