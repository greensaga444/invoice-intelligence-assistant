namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Input payload for running a downloader batch against multiple customers.
/// </summary>
public sealed record BatchInvoiceDownloadRequest(
    IReadOnlyList<CustomerRecord> Customers,
    string FolderAPath,
    string OutputNamingRule,
    string SessionToken,
    string? OfficialApiAppId = null,
    string? OfficialApiAccount = null,
    string? OfficialApiPassword = null);