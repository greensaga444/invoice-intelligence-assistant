namespace InvoiceDownloader;

public sealed record DownloaderIniSettings(
    string ModuleName,
    string FolderAPath,
    string FolderBPath,
    string CustomerListFilePath,
    string FileNamingRule,
    string ProcessorFileNamingRule,
    string ProcessorKnowledgeRepositoryFile,
    string MofAccount,
    string InvoiceDownloadMode,
    string OfficialApiBaseUrl,
    string OfficialApiAppId,
    string OfficialApiAppKey,
    string OfficialApiPassword);
