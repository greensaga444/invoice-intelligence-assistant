namespace InvoiceIntelligence.Core.Configuration;

public static class ConfigurationDefaults
{
    public const string KnowledgeRepositoryPath = "data\\knowledge\\knowledge-cases.json";
    public const string CustomerListFilePath = "config\\company-list.xlsx";
    public const string FolderCPath = "data\\folder-c-review";
    public const decimal LowConfidenceThreshold = 0.80m;
    public const string InvoiceOutputLabel = "SalesInvoiceDetail-T3";
    public const string AllowanceOutputLabel = "PurchaseVoucherDetail-T4";
    public const string InvoiceFileSuffix = "invoice";
    public const string AllowanceFileSuffix = "allowance";
    public const string OfficialDownloadMode = "official";
    public const string OfficialApiBaseUrl = "https://service-mc.einvoice.nat.gov.tw/ptl/portal/api";
}
