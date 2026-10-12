namespace InvoiceIntelligence.Core.Downloader;

public sealed record BatchInvoiceDownloadRequest(
    IReadOnlyList<CustomerRecord> Customers,
    string FolderAPath,
    string OutputNamingRule,
    string SessionToken,
    string? OfficialApiAppId = null,
    string? OfficialApiAccount = null,
    string? OfficialApiPassword = null);