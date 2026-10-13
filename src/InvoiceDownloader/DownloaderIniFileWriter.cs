using System.IO;
using System.Text;

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

        var sharedOfficialAppCredential = string.IsNullOrWhiteSpace(settings.OfficialApiAppId)
            ? settings.OfficialApiAppKey
            : settings.OfficialApiAppId;

        var lineBreak = Environment.NewLine;
        var builder = new StringBuilder();
        builder.Append("[module]").Append(lineBreak);
        builder.Append("name = ").Append(settings.ModuleName).Append(lineBreak);
        builder.Append(lineBreak);
        builder.Append("[paths]").Append(lineBreak);
        builder.Append("folderA = ").Append(settings.FolderAPath).Append(lineBreak);
        builder.Append("folderB = ").Append(settings.FolderBPath).Append(lineBreak);
        builder.Append(lineBreak);
        builder.Append("[output]").Append(lineBreak);
        builder.Append("fileNamingRule = ").Append(settings.FileNamingRule).Append(lineBreak);
        builder.Append(lineBreak);
        builder.Append("[mof]").Append(lineBreak);
        builder.Append("account = ").Append(settings.MofAccount).Append(lineBreak);
        builder.Append("invoiceDownloadMode = ").Append(settings.InvoiceDownloadMode).Append(lineBreak);
        builder.Append("officialApiBaseUrl = ").Append(settings.OfficialApiBaseUrl).Append(lineBreak);
        builder.Append("appId = ").Append(sharedOfficialAppCredential).Append(lineBreak);
        builder.Append("appKey = ").Append(sharedOfficialAppCredential).Append(lineBreak);

        File.WriteAllText(iniPath, builder.ToString(), Encoding.UTF8);
    }
}
