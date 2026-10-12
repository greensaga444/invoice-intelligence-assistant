namespace InvoiceIntelligence.Core.Downloader;

public sealed record BatchInvoiceDownloadRequest(
    IReadOnlyList<CustomerRecord> Customers,
    string FolderAPath,
    string OutputNamingRule,
    string SessionToken);