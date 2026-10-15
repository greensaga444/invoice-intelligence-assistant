namespace InvoiceLauncher;

public sealed record LauncherIniSettings(
    string ModuleName,
    string FolderAPath,
    string FolderBPath,
    string FileNamingRule,
    string ProcessorFileNamingRule,
    string ProcessorKnowledgeRepositoryFile,
    string MofAccount,
    string InvoiceDownloadMode,
    string OfficialApiBaseUrl,
    string OfficialApiAppId,
    string OfficialApiAppKey,
    string OfficialApiPassword);
