using System.IO;
using System.Text;
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

        var lineBreak = Environment.NewLine;
        var builder = new StringBuilder();
        builder.Append("[module]").Append(lineBreak);
        builder.Append("name = ").Append(existingConfig.ModuleName).Append(lineBreak);
        builder.Append(lineBreak);
        builder.Append("[paths]").Append(lineBreak);
        builder.Append("folderA = ").Append(folderAPath).Append(lineBreak);
        builder.Append("folderB = ").Append(folderBPath).Append(lineBreak);
        builder.Append(lineBreak);
        builder.Append("[output]").Append(lineBreak);
        builder.Append("fileNamingRule = ").Append(processorFileNamingRule).Append(lineBreak);
        builder.Append(lineBreak);
        builder.Append("[knowledge]").Append(lineBreak);
        builder.Append("repositoryFile = ").Append(processorKnowledgeRepositoryFile).Append(lineBreak);
        builder.Append(lineBreak);
        builder.Append("[mof]").Append(lineBreak);
        builder.Append("account = ").Append(processorMofAccount).Append(lineBreak);
        builder.Append("appId = ").Append(sharedOfficialAppCredential).Append(lineBreak);
        builder.Append("appKey = ").Append(sharedOfficialAppCredential).Append(lineBreak);

        File.WriteAllText(iniPath, builder.ToString(), Encoding.UTF8);
    }
}
