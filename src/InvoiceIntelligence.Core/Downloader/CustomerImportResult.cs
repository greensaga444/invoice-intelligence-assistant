namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Contains validated customer records imported from a source file.
/// </summary>
public sealed record CustomerImportResult(IReadOnlyList<CustomerRecord> Records);
