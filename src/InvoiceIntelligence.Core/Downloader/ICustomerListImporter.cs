namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Imports and validates customer list rows from an external file.
/// </summary>
public interface ICustomerListImporter
{
    CustomerImportResult Import(string filePath);
}
