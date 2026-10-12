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
    string? OfficialApiBaseUrl = null);
