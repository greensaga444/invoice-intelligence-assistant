using System.IO;
using InvoiceIntelligence.Core.Configuration;

namespace InvoiceDownloader;

internal static class DownloaderIniFileWriter
{
    public static void Write(string iniPath, DownloaderIniSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iniPath);
        ArgumentNullException.ThrowIfNull(settings);

        var directoryPath = Path.GetDirectoryName(iniPath)
            ?? throw new InvalidOperationException("無法解析設定檔路徑。");

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var encryptedPassword = MofPasswordProtector.Encrypt(settings.OfficialApiPassword);

        var content = IniFileContentBuilder.BuildDownloaderContent(
            moduleName: settings.ModuleName,
            folderAPath: settings.FolderAPath,
            folderBPath: settings.FolderBPath,
            customerListFilePath: settings.CustomerListFilePath,
            fileNamingRule: settings.FileNamingRule,
            invoiceFileSuffix: settings.InvoiceFileSuffix,
            allowanceFileSuffix: settings.AllowanceFileSuffix,
            mofAccount: settings.MofAccount,
            officialApiBaseUrl: settings.OfficialApiBaseUrl,
            officialApiAppId: settings.OfficialApiAppId,
            officialApiAppKey: settings.OfficialApiAppKey,
            officialApiPasswordEncrypted: encryptedPassword);

        File.WriteAllText(iniPath, content, System.Text.Encoding.UTF8);
    }
}
