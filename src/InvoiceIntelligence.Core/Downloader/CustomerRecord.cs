namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Normalized customer query parameters used by downloader workflows.
/// </summary>
public sealed record CustomerRecord(
    string TaxId,
    string CompanyName,
    DateOnly DateFrom,
    DateOnly DateTo);
