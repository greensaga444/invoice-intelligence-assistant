using System.Text;

namespace InvoiceIntelligence.Core.Configuration;

public static class IniFileContentBuilder
{
    public static string BuildDownloaderContent(
        string moduleName,
        string folderAPath,
        string folderBPath,
        string fileNamingRule,
        string mofAccount,
        string invoiceDownloadMode,
        string officialApiBaseUrl,
        string officialApiAppId,
        string officialApiAppKey,
        string officialApiPasswordEncrypted)
    {
        var resolvedOfficialApiAppId = ResolveOfficialApiAppId(officialApiAppId, officialApiAppKey);
        var resolvedOfficialApiAppKey = ResolveOfficialApiAppKey(officialApiAppId, officialApiAppKey);
        var lineBreak = Environment.NewLine;
        var builder = new StringBuilder();

        builder.Append("[module]").Append(lineBreak);
        builder.Append("name = ").Append(moduleName).Append(lineBreak);
        builder.Append(lineBreak);
        builder.Append("[paths]").Append(lineBreak);
        builder.Append("folderA = ").Append(folderAPath).Append(lineBreak);
        builder.Append("folderB = ").Append(folderBPath).Append(lineBreak);
        builder.Append(lineBreak);
        builder.Append("[output]").Append(lineBreak);
        builder.Append("fileNamingRule = ").Append(fileNamingRule).Append(lineBreak);
        builder.Append(lineBreak);
        builder.Append("[mof]").Append(lineBreak);
        builder.Append("account = ").Append(mofAccount).Append(lineBreak);
        builder.Append("invoiceDownloadMode = ").Append(invoiceDownloadMode).Append(lineBreak);
        builder.Append("officialApiBaseUrl = ").Append(officialApiBaseUrl).Append(lineBreak);
        builder.Append("appId = ").Append(resolvedOfficialApiAppId).Append(lineBreak);
        builder.Append("appKey = ").Append(resolvedOfficialApiAppKey).Append(lineBreak);
        builder.Append("passwordEncrypted = ").Append(officialApiPasswordEncrypted).Append(lineBreak);

        return builder.ToString();
    }

    public static string BuildProcessorContent(
        string moduleName,
        string folderAPath,
        string folderBPath,
        string fileNamingRule,
        string knowledgeRepositoryFile,
        string mofAccount,
        string officialApiAppId,
        string officialApiAppKey)
    {
        var resolvedOfficialApiAppId = ResolveOfficialApiAppId(officialApiAppId, officialApiAppKey);
        var resolvedOfficialApiAppKey = ResolveOfficialApiAppKey(officialApiAppId, officialApiAppKey);
        var lineBreak = Environment.NewLine;
        var builder = new StringBuilder();

        builder.Append("[module]").Append(lineBreak);
        builder.Append("name = ").Append(moduleName).Append(lineBreak);
        builder.Append(lineBreak);
        builder.Append("[paths]").Append(lineBreak);
        builder.Append("folderA = ").Append(folderAPath).Append(lineBreak);
        builder.Append("folderB = ").Append(folderBPath).Append(lineBreak);
        builder.Append(lineBreak);
        builder.Append("[output]").Append(lineBreak);
        builder.Append("fileNamingRule = ").Append(fileNamingRule).Append(lineBreak);
        builder.Append(lineBreak);
        builder.Append("[knowledge]").Append(lineBreak);
        builder.Append("repositoryFile = ").Append(knowledgeRepositoryFile).Append(lineBreak);
        builder.Append(lineBreak);
        builder.Append("[mof]").Append(lineBreak);
        builder.Append("account = ").Append(mofAccount).Append(lineBreak);
        builder.Append("appId = ").Append(resolvedOfficialApiAppId).Append(lineBreak);
        builder.Append("appKey = ").Append(resolvedOfficialApiAppKey).Append(lineBreak);

        return builder.ToString();
    }

    private static string ResolveOfficialApiAppId(string officialApiAppId, string officialApiAppKey)
    {
        return string.IsNullOrWhiteSpace(officialApiAppId)
            ? officialApiAppKey
            : officialApiAppId;
    }

    private static string ResolveOfficialApiAppKey(string officialApiAppId, string officialApiAppKey)
    {
        return string.IsNullOrWhiteSpace(officialApiAppKey)
            ? officialApiAppId
            : officialApiAppKey;
    }
}