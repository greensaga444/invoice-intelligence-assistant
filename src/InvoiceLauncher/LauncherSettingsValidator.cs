using InvoiceIntelligence.Core.Configuration;

namespace InvoiceLauncher;

internal static class LauncherSettingsValidator
{
    public static bool TryValidate(LauncherSettingsInput input, out string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(input);

        var requiredValues = new[]
        {
            input.ModuleName,
            input.FolderAPath,
            input.FolderBPath,
            input.FolderCPath,
            input.CustomerListFilePath,
            input.FileNamingRule,
            input.InvoiceFileSuffix,
            input.AllowanceFileSuffix,
            input.InvoiceOutputLabel,
            input.AllowanceOutputLabel,
            input.ProcessorKnowledgeRepositoryFile,
            input.MofAccount,
            input.OfficialApiAppId,
            input.OfficialApiAppKey
        };

        if (requiredValues.Any(string.IsNullOrWhiteSpace))
        {
            errorMessage = "請完整填寫所有欄位。";
            return false;
        }

        if (!ConfigurationValueParser.TryParseLowConfidenceThreshold(input.LowConfidenceThresholdText, out _))
        {
            errorMessage = "低信心門檻必須為 0 到 1 之間的小數。例：0.80";
            return false;
        }

        if (string.IsNullOrWhiteSpace(input.OfficialApiBaseUrl))
        {
            errorMessage = "請填寫官方 API Base URL。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(input.OfficialApiPassword))
        {
            errorMessage = "請填寫 MOF 密碼。";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}

internal sealed record LauncherSettingsInput(
    string ModuleName,
    string FolderAPath,
    string FolderBPath,
    string FolderCPath,
    string CustomerListFilePath,
    string FileNamingRule,
    string InvoiceFileSuffix,
    string AllowanceFileSuffix,
    string InvoiceOutputLabel,
    string AllowanceOutputLabel,
    string ProcessorKnowledgeRepositoryFile,
    string LowConfidenceThresholdText,
    string MofAccount,
    string OfficialApiBaseUrl,
    string OfficialApiAppId,
    string OfficialApiAppKey,
    string OfficialApiPassword);
