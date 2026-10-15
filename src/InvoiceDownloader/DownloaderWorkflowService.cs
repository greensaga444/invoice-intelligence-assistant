using InvoiceIntelligence.Core.Configuration;
using InvoiceIntelligence.Core.Downloader;

namespace InvoiceDownloader;

internal sealed class DownloaderWorkflowService
{
    public bool TryValidateAuthentication(
        ModuleConfiguration? configuration,
        IReadOnlyCollection<CustomerRecord> loadedCustomers,
        out string message)
    {
        if (configuration is null)
        {
            message = "設定尚未載入，無法進行 MOF 驗證。";
            return false;
        }

        if (loadedCustomers.Count == 0)
        {
            message = "請先匯入客戶清單，再進行 MOF 驗證。";
            return false;
        }

        message = string.Empty;
        return true;
    }

    public MofCredential BuildCredential(ModuleConfiguration configuration, string apiKey, string? configuredPassword, string inputPassword)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var resolvedPassword = string.IsNullOrWhiteSpace(inputPassword)
            ? configuredPassword ?? string.Empty
            : inputPassword;

        return new MofCredential(configuration.MofAccount, apiKey, resolvedPassword);
    }

    public bool TryValidateBatchDownload(
        ModuleConfiguration? configuration,
        IReadOnlyCollection<CustomerRecord> loadedCustomers,
        bool hasActiveSession,
        IBatchInvoiceDownloader? batchInvoiceDownloader,
        out string message)
    {
        if (configuration is null)
        {
            message = "設定尚未載入，無法執行批次下載。";
            return false;
        }

        if (loadedCustomers.Count == 0)
        {
            message = "請先匯入客戶清單，再執行批次下載。";
            return false;
        }

        if (!hasActiveSession)
        {
            message = "MOF 工作階段已失效，請重新驗證後再執行批次下載。";
            return false;
        }

        if (batchInvoiceDownloader is null)
        {
            message = "下載服務尚未初始化，請檢查設定後重啟。";
            return false;
        }

        message = string.Empty;
        return true;
    }

    public BatchInvoiceDownloadRequest BuildBatchRequest(
        ModuleConfiguration configuration,
        IReadOnlyList<CustomerRecord> loadedCustomers,
        string folderAPath,
        string sessionToken,
        string officialApiAppId,
        string? authenticatedPassword)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return new BatchInvoiceDownloadRequest(
            Customers: loadedCustomers,
            FolderAPath: folderAPath,
            OutputNamingRule: configuration.OutputNamingRule,
            SessionToken: sessionToken,
            OfficialApiAppId: officialApiAppId,
            OfficialApiAccount: configuration.MofAccount,
            OfficialApiPassword: authenticatedPassword,
            InvoiceFileSuffix: configuration.InvoiceFileSuffix,
            AllowanceFileSuffix: configuration.AllowanceFileSuffix);
    }
}
