using System.IO;
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

        var content = IniFileContentBuilder.BuildProcessorContent(
            moduleName: existingConfig.ModuleName,
            folderAPath: settings.FolderAPath,
            folderBPath: settings.FolderBPath,
            folderCPath: settings.FolderCPath,
            fileNamingRule: settings.FileNamingRule,
            invoiceFileSuffix: settings.InvoiceFileSuffix,
            allowanceFileSuffix: settings.AllowanceFileSuffix,
            lowConfidenceThreshold: settings.LowConfidenceThreshold,
            knowledgeRepositoryFile: settings.ProcessorKnowledgeRepositoryFile,
            invoiceOutputLabel: settings.InvoiceOutputLabel,
            allowanceOutputLabel: settings.AllowanceOutputLabel);

        File.WriteAllText(iniPath, content, System.Text.Encoding.UTF8);
    }
}
