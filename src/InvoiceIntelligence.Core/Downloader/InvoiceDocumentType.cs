namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Document type for downloaded invoice data.
/// </summary>
public enum InvoiceDocumentType
{
    /// <summary>
    /// Standard invoice documents.
    /// </summary>
    Invoice = 1,

    /// <summary>
    /// Allowance/reduction documents.
    /// </summary>
    Allowance = 2
}