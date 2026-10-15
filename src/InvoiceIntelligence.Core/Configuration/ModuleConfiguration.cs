namespace InvoiceIntelligence.Core.Configuration;

public sealed record ModuleConfiguration(
    string ModuleName,
    string FolderAPath,
    string FolderBPath,
    string OutputNamingRule,
    string MofAccount,
    string MofCredentialKey,
    string KnowledgeRepositoryPath,
    string InvoiceDownloadMode = "local",
    string? OfficialApiBaseUrl = null,
    string? OfficialApiAppId = null,
    string? OfficialApiAppKey = null,
    string? OfficialApiPasswordEncrypted = null,
    string CustomerListFilePath = "config\\company-list.xlsx",
    string FolderCPath = "data\\folder-c-review",
    decimal LowConfidenceThreshold = 0.80m);
