namespace InvoiceIntelligence.Core.Downloader;

public interface ICustomerListImporter
{
    CustomerImportResult Import(string filePath);
}
