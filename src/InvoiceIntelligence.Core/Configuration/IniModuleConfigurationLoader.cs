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
    private const string DefaultInvoiceOutputLabel = "SalesInvoiceDetail-T3";
    private const string DefaultAllowanceOutputLabel = "PurchaseVoucherDetail-T4";
    private const string DefaultInvoiceFileSuffix = "invoice";
    private const string DefaultAllowanceFileSuffix = "allowance";
    private const string OfficialDownloadMode = "official";
    private const string DefaultOfficialApiBaseUrl = "https://service-mc.einvoice.nat.gov.tw/ptl/portal/api";

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
        var invoiceOutputLabel = TrimOptional(iniData.GetOptional("output", "invoiceOutputLabel"))
            ?? DefaultInvoiceOutputLabel;
        var allowanceOutputLabel = TrimOptional(iniData.GetOptional("output", "allowanceOutputLabel"))
            ?? DefaultAllowanceOutputLabel;
        var invoiceFileSuffix = ResolveFileSuffix(
            iniData.GetOptional("output", "invoiceFileSuffix"),
            DefaultInvoiceFileSuffix,
            "output.invoiceFileSuffix");
        var allowanceFileSuffix = ResolveFileSuffix(
            iniData.GetOptional("output", "allowanceFileSuffix"),
            DefaultAllowanceFileSuffix,
            "output.allowanceFileSuffix");
        var mofAccount = iniData.GetRequired("mof", "account");
        var officialApiAppKey = iniData.GetRequired("mof", "appKey").Trim();
        var officialApiAppId = TrimOptional(iniData.GetOptional("mof", "appId")) ?? officialApiAppKey;
        var mofCredentialKey = officialApiAppKey;
        var knowledgeRepositoryPath = iniData.GetOptional("knowledge", "repositoryFile")
            ?? DefaultKnowledgeRepositoryPath;
        var customerListFilePath = TrimOptional(iniData.GetOptional("input", "customerListFile"))
            ?? DefaultCustomerListFilePath;
        var configuredInvoiceDownloadMode = iniData.GetOptional("mof", "invoiceDownloadMode");
        var invoiceDownloadMode = NormalizeInvoiceDownloadMode(configuredInvoiceDownloadMode);
        var officialApiBaseUrl = ResolveOfficialApiBaseUrl(
            iniData.GetOptional("mof", "officialApiBaseUrl"),
            isInvoiceDownloadModeExplicitlyConfigured: !string.IsNullOrWhiteSpace(configuredInvoiceDownloadMode));
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
            lowConfidenceThreshold,
            invoiceOutputLabel,
            allowanceOutputLabel,
            invoiceFileSuffix,
            allowanceFileSuffix);
    }

    private static string ResolveFileSuffix(string? configuredValue, string fallback, string keyName)
    {
        var normalized = TrimOptional(configuredValue);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return fallback;
        }

        var suffix = normalized;
        if (suffix.StartsWith('-'))
        {
            suffix = suffix[1..];
        }

        if (string.IsNullOrWhiteSpace(suffix))
        {
            throw new InvalidDataException($"{keyName} 不可為空白。");
        }

        foreach (var character in suffix)
        {
            if (char.IsLetterOrDigit(character) || character == '-' || character == '_')
            {
                continue;
            }

            throw new InvalidDataException($"{keyName} 僅允許英數、'-'、'_' 字元。");
        }

        return suffix;
    }

    private static string NormalizeInvoiceDownloadMode(string? configuredMode)
    {
        if (string.IsNullOrWhiteSpace(configuredMode))
        {
            return OfficialDownloadMode;
        }

        var normalized = configuredMode.Trim().ToLowerInvariant();
        if (normalized != OfficialDownloadMode)
        {
            throw new InvalidDataException("mof.invoiceDownloadMode 僅支援 official。");
        }

        return normalized;
    }

    private static string? TrimOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string ResolveOfficialApiBaseUrl(
        string? configuredBaseUrl,
        bool isInvoiceDownloadModeExplicitlyConfigured)
    {
        if (string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            if (isInvoiceDownloadModeExplicitlyConfigured)
            {
                throw new InvalidDataException("當 mof.invoiceDownloadMode=official 時，mof.officialApiBaseUrl 不可為空白。");
            }

            return DefaultOfficialApiBaseUrl;
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
