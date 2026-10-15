namespace InvoiceLauncher;

public sealed record LauncherIniSettings(
    string ModuleName,
    string FolderAPath,
    string FolderBPath,
    string FolderCPath,
    string CustomerListFilePath,
    string FileNamingRule,
    string ProcessorFileNamingRule,
    string ProcessorKnowledgeRepositoryFile,
    decimal LowConfidenceThreshold,
    string MofAccount,
    string InvoiceDownloadMode,
    string OfficialApiBaseUrl,
    string OfficialApiAppId,
    string OfficialApiAppKey,
    string OfficialApiPassword);
