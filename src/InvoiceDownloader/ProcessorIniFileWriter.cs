using System.IO;
using InvoiceIntelligence.Core.Configuration;

namespace InvoiceDownloader;

internal static class ProcessorIniFileWriter
{
    public static void Write(
        string iniPath,
        ModuleConfiguration existingConfig,
        string folderAPath,
        string folderBPath,
        string processorFileNamingRule,
        string processorKnowledgeRepositoryFile,
        string processorMofAccount,
        string sharedOfficialAppCredential)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iniPath);
        ArgumentNullException.ThrowIfNull(existingConfig);
        ArgumentException.ThrowIfNullOrWhiteSpace(folderAPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(folderBPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(processorFileNamingRule);
        ArgumentException.ThrowIfNullOrWhiteSpace(processorKnowledgeRepositoryFile);
        ArgumentException.ThrowIfNullOrWhiteSpace(processorMofAccount);
        ArgumentException.ThrowIfNullOrWhiteSpace(sharedOfficialAppCredential);

        var directoryPath = Path.GetDirectoryName(iniPath)
            ?? throw new InvalidOperationException("無法解析設定檔路徑。");

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var content = IniFileContentBuilder.BuildProcessorContent(
            moduleName: existingConfig.ModuleName,
            folderAPath: folderAPath,
            folderBPath: folderBPath,
            fileNamingRule: processorFileNamingRule,
            knowledgeRepositoryFile: processorKnowledgeRepositoryFile,
            mofAccount: processorMofAccount,
            officialApiAppId: sharedOfficialAppCredential,
            officialApiAppKey: sharedOfficialAppCredential);

        File.WriteAllText(iniPath, content, System.Text.Encoding.UTF8);
    }
}
