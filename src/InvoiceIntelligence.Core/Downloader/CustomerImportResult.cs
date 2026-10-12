namespace InvoiceIntelligence.Core.Downloader;

public sealed record CustomerImportResult(IReadOnlyList<CustomerRecord> Records);
