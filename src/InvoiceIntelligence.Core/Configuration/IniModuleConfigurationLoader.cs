using System.IO;
using System.Globalization;

namespace InvoiceIntelligence.Core.Configuration;

public interface IModuleConfigurationLoader
{
    ModuleConfiguration Load(string iniPath);
}

public sealed class IniModuleConfigurationLoader : IModuleConfigurationLoader
{
    private const string DefaultKnowledgeRepositoryPath = "data\\knowledge\\knowledge-cases.json";
    private const string DefaultCustomerListFilePath = "config\\company-list.xlsx";
    private const string DefaultFolderCPath = "data\\folder-c-review";
    private const decimal DefaultLowConfidenceThreshold = 0.80m;
    private const string LocalDownloadMode = "local";
    private const string OfficialDownloadMode = "official";

    public ModuleConfiguration Load(string iniPath)
    {
        if (string.IsNullOrWhiteSpace(iniPath))
        {
            throw new ArgumentException("INI 路徑不可為空白。", nameof(iniPath));
        }

        if (!File.Exists(iniPath))
        {
            throw new FileNotFoundException("找不到 INI 設定檔。", iniPath);
        }

        var iniData = IniFileParser.Parse(File.ReadLines(iniPath));

        var moduleName = iniData.GetRequired("module", "name");
        var folderAPath = iniData.GetRequired("paths", "folderA");
        var folderBPath = iniData.GetRequired("paths", "folderB");
        var folderCPath = TrimOptional(iniData.GetOptional("paths", "folderC"))
            ?? DefaultFolderCPath;
        var outputNamingRule = iniData.GetRequired("output", "fileNamingRule");
        var mofAccount = iniData.GetRequired("mof", "account");
        var officialApiAppKey = iniData.GetRequired("mof", "appKey").Trim();
        var officialApiAppId = TrimOptional(iniData.GetOptional("mof", "appId")) ?? officialApiAppKey;
        var mofCredentialKey = officialApiAppKey;
        var knowledgeRepositoryPath = iniData.GetOptional("knowledge", "repositoryFile")
            ?? DefaultKnowledgeRepositoryPath;
        var customerListFilePath = TrimOptional(iniData.GetOptional("input", "customerListFile"))
            ?? DefaultCustomerListFilePath;
        var invoiceDownloadMode = NormalizeInvoiceDownloadMode(iniData.GetOptional("mof", "invoiceDownloadMode"));
        var officialApiBaseUrl = ResolveOfficialApiBaseUrl(invoiceDownloadMode, iniData.GetOptional("mof", "officialApiBaseUrl"));
        var officialApiPasswordEncrypted = TrimOptional(iniData.GetOptional("mof", "passwordEncrypted"));
        var lowConfidenceThreshold = ResolveLowConfidenceThreshold(iniData.GetOptional("review", "confidenceThreshold"));

        NamingRuleValidator.Validate(outputNamingRule);

        return new ModuleConfiguration(
            moduleName,
            folderAPath,
            folderBPath,
            outputNamingRule,
            mofAccount,
            mofCredentialKey,
            knowledgeRepositoryPath,
            invoiceDownloadMode,
            officialApiBaseUrl,
            officialApiAppId,
            officialApiAppKey,
            officialApiPasswordEncrypted,
                customerListFilePath,
                folderCPath,
                lowConfidenceThreshold);
    }

    private static string NormalizeInvoiceDownloadMode(string? configuredMode)
    {
        if (string.IsNullOrWhiteSpace(configuredMode))
        {
            return LocalDownloadMode;
        }

        var normalized = configuredMode.Trim().ToLowerInvariant();
        if (normalized is not (LocalDownloadMode or OfficialDownloadMode))
        {
            throw new InvalidDataException("mof.invoiceDownloadMode 僅支援 local 或 official。");
        }

        return normalized;
    }

    private static string? TrimOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? ResolveOfficialApiBaseUrl(string invoiceDownloadMode, string? configuredBaseUrl)
    {
        if (invoiceDownloadMode != OfficialDownloadMode)
        {
            return string.IsNullOrWhiteSpace(configuredBaseUrl) ? null : configuredBaseUrl.Trim();
        }

        if (string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            throw new InvalidDataException("當 mof.invoiceDownloadMode=official 時，mof.officialApiBaseUrl 不可為空白。");
        }

        var normalized = configuredBaseUrl.Trim();
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out _))
        {
            throw new InvalidDataException("mof.officialApiBaseUrl 必須為合法的絕對網址。");
        }

        return normalized;
    }

    private static decimal ResolveLowConfidenceThreshold(string? configuredThreshold)
    {
        if (string.IsNullOrWhiteSpace(configuredThreshold))
        {
            return DefaultLowConfidenceThreshold;
        }

        var normalized = configuredThreshold.Trim();

        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var threshold)
            && !decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.CurrentCulture, out threshold))
        {
            throw new InvalidDataException("review.confidenceThreshold 必須為 0 到 1 之間的小數。例：0.80");
        }

        if (threshold is < 0m or > 1m)
        {
            throw new InvalidDataException("review.confidenceThreshold 必須介於 0 與 1 之間。");
        }

        return decimal.Round(threshold, 2, MidpointRounding.AwayFromZero);
    }
}
