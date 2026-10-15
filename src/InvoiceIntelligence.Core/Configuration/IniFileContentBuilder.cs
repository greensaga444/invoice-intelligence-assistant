using System.Globalization;

namespace InvoiceIntelligence.Core.Configuration;

public static class IniFileContentBuilder
{
    public static string BuildDownloaderContent(
        string moduleName,
        string folderAPath,
        string folderBPath,
        string customerListFilePath,
        string fileNamingRule,
        string invoiceFileSuffix,
        string allowanceFileSuffix,
        string mofAccount,
        string officialApiBaseUrl,
        string officialApiAppId,
        string officialApiAppKey,
        string officialApiPasswordEncrypted)
    {
        var resolvedOfficialApiAppId = MofApiCredentialResolver.ResolveAppId(officialApiAppId, officialApiAppKey);
        var resolvedOfficialApiAppKey = MofApiCredentialResolver.ResolveAppKey(officialApiAppId, officialApiAppKey);
        var builder = new IniSectionContentBuilder();
        builder.BeginSection("module")
            .Add("name", moduleName)
            .AddBlankLine()
            .BeginSection("paths")
            .Add("folderA", folderAPath)
            .Add("folderB", folderBPath)
            .AddBlankLine()
            .BeginSection("input")
            .Add("customerListFile", customerListFilePath)
            .AddBlankLine()
            .BeginSection("output")
            .Add("fileNamingRule", fileNamingRule)
            .Add("invoiceFileSuffix", invoiceFileSuffix)
            .Add("allowanceFileSuffix", allowanceFileSuffix)
            .AddBlankLine()
            .BeginSection("mof")
            .Add("account", mofAccount)
            .Add("officialApiBaseUrl", officialApiBaseUrl)
            .Add("appId", resolvedOfficialApiAppId)
            .Add("appKey", resolvedOfficialApiAppKey)
            .Add("passwordEncrypted", officialApiPasswordEncrypted);

        return builder.ToString();
    }

    public static string BuildProcessorContent(
        string moduleName,
        string folderAPath,
        string folderBPath,
        string folderCPath,
        string fileNamingRule,
        string invoiceFileSuffix,
        string allowanceFileSuffix,
        decimal lowConfidenceThreshold,
        string knowledgeRepositoryFile,
        string mofAccount,
        string officialApiAppId,
        string officialApiAppKey,
        string invoiceOutputLabel = ConfigurationDefaults.InvoiceOutputLabel,
        string allowanceOutputLabel = ConfigurationDefaults.AllowanceOutputLabel)
    {
        var resolvedOfficialApiAppId = MofApiCredentialResolver.ResolveAppId(officialApiAppId, officialApiAppKey);
        var resolvedOfficialApiAppKey = MofApiCredentialResolver.ResolveAppKey(officialApiAppId, officialApiAppKey);
        var builder = new IniSectionContentBuilder();
        builder.BeginSection("module")
            .Add("name", moduleName)
            .AddBlankLine()
            .BeginSection("paths")
            .Add("folderA", folderAPath)
            .Add("folderB", folderBPath)
            .Add("folderC", folderCPath)
            .AddBlankLine()
            .BeginSection("output")
            .Add("fileNamingRule", fileNamingRule)
            .Add("invoiceOutputLabel", invoiceOutputLabel)
            .Add("allowanceOutputLabel", allowanceOutputLabel)
            .Add("invoiceFileSuffix", invoiceFileSuffix)
            .Add("allowanceFileSuffix", allowanceFileSuffix)
            .AddBlankLine()
            .BeginSection("review")
            .Add("confidenceThreshold", lowConfidenceThreshold.ToString("0.00", CultureInfo.InvariantCulture))
            .AddBlankLine()
            .BeginSection("knowledge")
            .Add("repositoryFile", knowledgeRepositoryFile)
            .AddBlankLine()
            .BeginSection("mof")
            .Add("account", mofAccount)
            .Add("appId", resolvedOfficialApiAppId)
            .Add("appKey", resolvedOfficialApiAppKey);

        return builder.ToString();
    }
}