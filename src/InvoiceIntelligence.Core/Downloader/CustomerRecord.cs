namespace InvoiceIntelligence.Core.Downloader;

public sealed record CustomerRecord(
    string TaxId,
    string CompanyName,
    DateOnly DateFrom,
    DateOnly DateTo);
