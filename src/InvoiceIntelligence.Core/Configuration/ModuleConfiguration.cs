namespace InvoiceIntelligence.Core.Configuration;

public sealed record ModuleConfiguration(
    string ModuleName,
    string FolderAPath,
    string FolderBPath,
    string OutputNamingRule,
    string MofAccount,
    string MofCredentialKey,
    string KnowledgeRepositoryPath,
    string InvoiceDownloadMode = ConfigurationDefaults.OfficialDownloadMode,
    string? OfficialApiBaseUrl = null,
    string? OfficialApiAppId = null,
    string? OfficialApiAppKey = null,
    string? OfficialApiPasswordEncrypted = null,
    string CustomerListFilePath = ConfigurationDefaults.CustomerListFilePath,
    string FolderCPath = ConfigurationDefaults.FolderCPath,
    decimal LowConfidenceThreshold = ConfigurationDefaults.LowConfidenceThreshold,
    string InvoiceOutputLabel = ConfigurationDefaults.InvoiceOutputLabel,
    string AllowanceOutputLabel = ConfigurationDefaults.AllowanceOutputLabel,
    string InvoiceFileSuffix = ConfigurationDefaults.InvoiceFileSuffix,
    string AllowanceFileSuffix = ConfigurationDefaults.AllowanceFileSuffix);
