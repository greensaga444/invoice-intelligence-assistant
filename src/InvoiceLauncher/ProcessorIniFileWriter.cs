using System.IO;
using System.Text;
using InvoiceIntelligence.Core.Configuration;

namespace InvoiceLauncher;

internal static class ProcessorIniFileWriter
{
    public static void Write(
        string iniPath,
        ModuleConfiguration existingConfig,
        LauncherIniSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iniPath);
        ArgumentNullException.ThrowIfNull(existingConfig);
        ArgumentNullException.ThrowIfNull(settings);

        var directoryPath = Path.GetDirectoryName(iniPath)
            ?? throw new InvalidOperationException("無法解析設定檔路徑。");

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var sharedOfficialAppCredential = string.IsNullOrWhiteSpace(settings.OfficialApiAppId)
            ? settings.OfficialApiAppKey
            : settings.OfficialApiAppId;

        var lineBreak = Environment.NewLine;
        var builder = new StringBuilder();
        builder.Append("[module]").Append(lineBreak);
        builder.Append("name = ").Append(existingConfig.ModuleName).Append(lineBreak);
        builder.Append(lineBreak);
        builder.Append("[paths]").Append(lineBreak);
        builder.Append("folderA = ").Append(settings.FolderAPath).Append(lineBreak);
        builder.Append("folderB = ").Append(settings.FolderBPath).Append(lineBreak);
        builder.Append(lineBreak);
        builder.Append("[output]").Append(lineBreak);
        builder.Append("fileNamingRule = ").Append(settings.ProcessorFileNamingRule).Append(lineBreak);
        builder.Append(lineBreak);
        builder.Append("[knowledge]").Append(lineBreak);
        builder.Append("repositoryFile = ").Append(settings.ProcessorKnowledgeRepositoryFile).Append(lineBreak);
        builder.Append(lineBreak);
        builder.Append("[mof]").Append(lineBreak);
        builder.Append("account = ").Append(settings.MofAccount).Append(lineBreak);
        builder.Append("appId = ").Append(sharedOfficialAppCredential).Append(lineBreak);
        builder.Append("appKey = ").Append(sharedOfficialAppCredential).Append(lineBreak);

        File.WriteAllText(iniPath, builder.ToString(), Encoding.UTF8);
    }
}
