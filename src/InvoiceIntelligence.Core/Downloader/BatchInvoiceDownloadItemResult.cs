namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Represents one customer's download outcome within a batch execution.
/// </summary>
public sealed record BatchInvoiceDownloadItemResult(
    CustomerRecord Customer,
    bool IsSuccess,
    string Message,
    string? OutputFilePath,
    IReadOnlyList<string>? OutputFilePaths = null);