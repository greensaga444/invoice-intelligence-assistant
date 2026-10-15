using System.IO;
using System.Net.Http;
using System.Windows;
using InvoiceIntelligence.Core.Configuration;
using InvoiceIntelligence.Core.Downloader;
using Microsoft.Win32;

namespace InvoiceDownloader;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ModuleConfiguration? _configuration;
    private readonly string _solutionRootPath;
    private readonly ICustomerListImporter _customerListImporter;
    private readonly IMofAuthenticator _mofAuthenticator;
    private readonly DownloaderWorkflowService _workflowService;
    private readonly IBatchInvoiceDownloader? _batchInvoiceDownloader;
    private readonly HttpClient? _officialApiHttpClient;
    private IReadOnlyList<CustomerRecord> _loadedCustomers = [];
    private string? _sessionToken;
    private DateTimeOffset? _sessionExpiresAt;
    private string? _configuredMofPassword;
    private string? _authenticatedPassword;

    public MainWindow(
        ModuleConfiguration? configuration,
        string statusMessage,
        string solutionRootPath)
    {
        InitializeComponent();

        _configuration = configuration;
        _solutionRootPath = solutionRootPath;
        _customerListImporter = new CustomerListExcelImporter();
        _mofAuthenticator = new LocalMofAuthenticator();
        _workflowService = new DownloaderWorkflowService();

        if (_configuration is not null)
        {
            (_batchInvoiceDownloader, _officialApiHttpClient) = CreateBatchDownloader(_configuration);
        }

        StartupStatusText.Text = statusMessage;
        StartBatchDownloadButton.IsEnabled = false;

        if (_configuration is null)
        {
            ImportCustomerFileButton.IsEnabled = false;
            AuthenticateButton.IsEnabled = false;
            StartBatchDownloadButton.IsEnabled = false;
            return;
        }

        CustomerFilePathTextBox.Text = ResolveConfiguredPath(_configuration.CustomerListFilePath);

        MofAccountValueText.Text = _configuration.MofAccount;
        MofApiKeyValueText.Text = MaskApiKey(GetEffectiveMofApiKey());

        if (MofPasswordProtector.TryDecrypt(_configuration.OfficialApiPasswordEncrypted, out var configuredPassword))
        {
            _configuredMofPassword = configuredPassword;
            MofPasswordBox.Password = configuredPassword;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _officialApiHttpClient?.Dispose();
        base.OnClosed(e);
    }

    private void BrowseCustomerFileButton_Click(object sender, RoutedEventArgs e)
    {
        var configuredPath = CustomerFilePathTextBox.Text.Trim();

        var dialog = new OpenFileDialog
        {
            Filter = "Excel 檔案 (*.xlsx)|*.xlsx",
            CheckFileExists = true,
            Multiselect = false,
            Title = "選擇客戶清單檔案"
        };

        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            var defaultConfigFolderPath = Path.Combine(_solutionRootPath, "config");
            if (Directory.Exists(defaultConfigFolderPath))
            {
                dialog.InitialDirectory = defaultConfigFolderPath;
            }
        }
        else
        {
            var resolvedPath = ResolveConfiguredPath(configuredPath);
            if (Directory.Exists(resolvedPath))
            {
                dialog.InitialDirectory = resolvedPath;
            }
            else
            {
                var directoryPath = Path.GetDirectoryName(resolvedPath);
                if (!string.IsNullOrWhiteSpace(directoryPath) && Directory.Exists(directoryPath))
                {
                    dialog.InitialDirectory = directoryPath;
                    dialog.FileName = Path.GetFileName(resolvedPath);
                }
            }
        }

        if (dialog.ShowDialog() == true)
        {
            CustomerFilePathTextBox.Text = dialog.FileName;
        }
    }

    private void ImportCustomerFileButton_Click(object sender, RoutedEventArgs e)
    {
        var configuredPath = CustomerFilePathTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            ImportResultText.Text = "請先選擇客戶清單 Excel 檔案。";
            return;
        }

        var filePath = ResolveConfiguredPath(configuredPath);

        try
        {
            var result = _customerListImporter.Import(filePath);
            _loadedCustomers = result.Records;
            CustomerDataGrid.ItemsSource = _loadedCustomers;
            StartBatchDownloadButton.IsEnabled = HasActiveSession();
            CustomerFilePathTextBox.Text = filePath;

            ImportResultText.Text = $"匯入成功，共載入 {_loadedCustomers.Count} 筆客戶資料。";
        }
        catch (Exception ex)
        {
            _loadedCustomers = [];
            CustomerDataGrid.ItemsSource = null;
            StartBatchDownloadButton.IsEnabled = false;
            ImportResultText.Text = $"匯入失敗: {ex.Message}";
        }
    }

    private void AuthenticateButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_workflowService.TryValidateAuthentication(_configuration, _loadedCustomers, out var authMessage))
        {
            MofAuthResultText.Text = authMessage;
            return;
        }

        var credential = _workflowService.BuildCredential(
            _configuration!,
            GetEffectiveMofApiKey(),
            _configuredMofPassword,
            MofPasswordBox.Password);

        var authResult = _mofAuthenticator.Authenticate(credential);
        if (!authResult.IsSuccess)
        {
            _sessionToken = null;
            _sessionExpiresAt = null;
            _authenticatedPassword = null;
            StartBatchDownloadButton.IsEnabled = false;
            MofAuthResultText.Text = $"驗證失敗: {authResult.Message}";
            return;
        }

        _sessionToken = authResult.SessionToken;
        _sessionExpiresAt = authResult.ExpiresAt;
        _configuredMofPassword = credential.Password;
        _authenticatedPassword = credential.Password;
        StartBatchDownloadButton.IsEnabled = _loadedCustomers.Count > 0;

        var expiresAtText = authResult.ExpiresAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "無";
        MofAuthResultText.Text = $"{authResult.Message} 工作階段有效至: {expiresAtText}";

        MofPasswordBox.Clear();
    }

    private async void StartBatchDownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_workflowService.TryValidateBatchDownload(
                _configuration,
                _loadedCustomers,
                HasActiveSession(),
                _batchInvoiceDownloader,
                out var batchValidationMessage))
        {
            StartBatchDownloadButton.IsEnabled = HasActiveSession() && _loadedCustomers.Count > 0;
            BatchDownloadResultText.Text = batchValidationMessage;
            return;
        }

        StartBatchDownloadButton.IsEnabled = false;

        try
        {
            var configuration = _configuration!;
            var batchDownloader = _batchInvoiceDownloader!;
            var folderAPath = ResolveConfiguredPath(configuration.FolderAPath);
            var request = _workflowService.BuildBatchRequest(
                configuration,
                _loadedCustomers,
                folderAPath,
                _sessionToken!,
                GetEffectiveOfficialApiAppId(),
                _authenticatedPassword);

            var result = await batchDownloader.DownloadAsync(request);

            var successCount = result.Items.Count(item => item.IsSuccess);
            var failureCount = result.Items.Count - successCount;

            BatchDownloadResultText.Text =
                $"批次下載完成：成功 {successCount} 筆，失敗 {failureCount} 筆。\n" +
                $"模式: {GetDownloadModeLabel()}\n" +
                $"原始發票資料夾 (Folder A): {folderAPath}\n" +
                $"下載記錄: {result.LogFilePath}";
        }
        catch (Exception ex)
        {
            BatchDownloadResultText.Text = $"批次下載失敗: {ex.Message}";
        }
        finally
        {
            StartBatchDownloadButton.IsEnabled = _loadedCustomers.Count > 0 && HasActiveSession();
        }
    }

    private static string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "(未設定)";
        }

        if (apiKey.Length <= 4)
        {
            return new string('*', apiKey.Length);
        }

        return $"{apiKey[..2]}***{apiKey[^2..]}";
    }

    private bool HasActiveSession()
    {
        return !string.IsNullOrWhiteSpace(_sessionToken)
            && _sessionExpiresAt is not null
            && _sessionExpiresAt > DateTimeOffset.UtcNow;
    }

    private string GetEffectiveMofApiKey()
    {
        return MofApiCredentialResolver.ResolveAppKey(
            _configuration?.OfficialApiAppId,
            _configuration?.OfficialApiAppKey);
    }

    private string GetEffectiveOfficialApiAppId()
    {
        return MofApiCredentialResolver.ResolveAppId(
            _configuration?.OfficialApiAppId,
            _configuration?.OfficialApiAppKey);
    }

    private string ResolveConfiguredPath(string configuredPath)
    {
        return PathResolutionHelper.ResolveConfiguredPath(_solutionRootPath, configuredPath);
    }

    private static (IBatchInvoiceDownloader Downloader, HttpClient? OfficialApiHttpClient) CreateBatchDownloader(ModuleConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.OfficialApiBaseUrl))
        {
            throw new InvalidOperationException("官方下載模式缺少 officialApiBaseUrl 設定。請更新 INI 後重啟。");
        }

        var httpClient = new HttpClient();
        var apiClient = new MofOfficialHttpInvoiceApiClient(httpClient, configuration.OfficialApiBaseUrl);
        var officialDownloader = new OfficialInvoiceBatchDownloader(apiClient);
        var exporter = new OfficialInvoiceFolderAExcelExporter();
        var batchDownloader = new OfficialBatchInvoiceDownloader(
            officialDownloader,
            exporter,
            scopeSelection: OfficialInvoiceScopeSelection.Default);

        return (batchDownloader, httpClient);
    }

    private string GetDownloadModeLabel()
    {
        return "官方 API";
    }
}