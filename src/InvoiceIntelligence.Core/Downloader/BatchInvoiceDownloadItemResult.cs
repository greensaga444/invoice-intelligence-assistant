namespace InvoiceIntelligence.Core.Downloader;

public sealed record BatchInvoiceDownloadItemResult(
    CustomerRecord Customer,
    bool IsSuccess,
    string Message,
    string? OutputFilePath);