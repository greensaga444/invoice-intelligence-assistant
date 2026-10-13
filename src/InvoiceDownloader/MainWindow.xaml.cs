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
    private readonly string _downloaderConfigPath;
    private readonly string _processorConfigPath;
    private readonly ICustomerListImporter _customerListImporter;
    private readonly IMofAuthenticator _mofAuthenticator;
    private readonly IBatchInvoiceDownloader _batchInvoiceDownloader;
    private readonly HttpClient? _officialApiHttpClient;
    private IReadOnlyList<CustomerRecord> _loadedCustomers = [];
    private string? _sessionToken;
    private DateTimeOffset? _sessionExpiresAt;
    private string? _authenticatedPassword;

    public MainWindow(
        ModuleConfiguration? configuration,
        string statusMessage,
        string solutionRootPath,
        string downloaderConfigPath,
        string processorConfigPath)
    {
        InitializeComponent();

        _configuration = configuration;
        _solutionRootPath = solutionRootPath;
        _downloaderConfigPath = downloaderConfigPath;
        _processorConfigPath = processorConfigPath;
        _customerListImporter = new CustomerListExcelImporter();
        _mofAuthenticator = new LocalMofAuthenticator();

        if (_configuration is null)
        {
            _batchInvoiceDownloader = new LocalBatchInvoiceDownloader();
        }
        else
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

        MofAccountValueText.Text = _configuration.MofAccount;
        MofApiKeyValueText.Text = MaskApiKey(GetEffectiveMofApiKey());
    }

    protected override void OnClosed(EventArgs e)
    {
        _officialApiHttpClient?.Dispose();
        base.OnClosed(e);
    }

    private void BrowseCustomerFileButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Excel 檔案 (*.xlsx)|*.xlsx",
            CheckFileExists = true,
            Multiselect = false,
            Title = "選擇客戶清單檔案"
        };

        if (dialog.ShowDialog() == true)
        {
            CustomerFilePathTextBox.Text = dialog.FileName;
        }
    }

    private void ImportCustomerFileButton_Click(object sender, RoutedEventArgs e)
    {
        var filePath = CustomerFilePathTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            ImportResultText.Text = "請先選擇客戶清單 Excel 檔案。";
            return;
        }

        try
        {
            var result = _customerListImporter.Import(filePath);
            _loadedCustomers = result.Records;
            CustomerDataGrid.ItemsSource = _loadedCustomers;
            StartBatchDownloadButton.IsEnabled = HasActiveSession();

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
        if (_configuration is null)
        {
            MofAuthResultText.Text = "設定尚未載入，無法進行 MOF 驗證。";
            return;
        }

        if (_loadedCustomers.Count == 0)
        {
            MofAuthResultText.Text = "請先匯入客戶清單，再進行 MOF 驗證。";
            return;
        }

        var credential = new MofCredential(
            _configuration.MofAccount,
            GetEffectiveMofApiKey(),
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
        _authenticatedPassword = credential.Password;
        StartBatchDownloadButton.IsEnabled = _loadedCustomers.Count > 0;

        var expiresAtText = authResult.ExpiresAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "無";
        MofAuthResultText.Text = $"{authResult.Message} 工作階段有效至: {expiresAtText}";

        MofPasswordBox.Clear();
    }

    private void StartBatchDownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (_configuration is null)
        {
            BatchDownloadResultText.Text = "設定尚未載入，無法執行批次下載。";
            return;
        }

        if (_loadedCustomers.Count == 0)
        {
            BatchDownloadResultText.Text = "請先匯入客戶清單，再執行批次下載。";
            return;
        }

        if (!HasActiveSession())
        {
            StartBatchDownloadButton.IsEnabled = false;
            BatchDownloadResultText.Text = "MOF 工作階段已失效，請重新驗證後再執行批次下載。";
            return;
        }

        try
        {
            var folderAPath = ResolveConfiguredPath(_configuration.FolderAPath);
            var request = new BatchInvoiceDownloadRequest(
                Customers: _loadedCustomers,
                FolderAPath: folderAPath,
                OutputNamingRule: _configuration.OutputNamingRule,
                SessionToken: _sessionToken!,
                OfficialApiAppId: GetEffectiveOfficialApiAppId(),
                OfficialApiAccount: _configuration.MofAccount,
                OfficialApiPassword: _authenticatedPassword);

            var result = _batchInvoiceDownloader.Download(request);

            var successCount = result.Items.Count(item => item.IsSuccess);
            var failureCount = result.Items.Count - successCount;

            BatchDownloadResultText.Text =
                $"批次下載完成：成功 {successCount} 筆，失敗 {failureCount} 筆。\n" +
                $"模式: {GetDownloadModeLabel()}\n" +
                $"資料夾 A: {folderAPath}\n" +
                $"下載記錄: {result.LogFilePath}";
        }
        catch (Exception ex)
        {
            BatchDownloadResultText.Text = $"批次下載失敗: {ex.Message}";
        }
    }

    private void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new DownloaderSettingsWindow(BuildSettingsFromCurrentState())
        {
            Owner = this
        };

        if (settingsWindow.ShowDialog() != true || settingsWindow.SavedSettings is null)
        {
            return;
        }

        try
        {
            DownloaderIniFileWriter.Write(_downloaderConfigPath, settingsWindow.SavedSettings);
            SyncProcessorSettings(settingsWindow.SavedSettings);

            MessageBox.Show(
                this,
                $"設定已儲存。\nDownloader: {_downloaderConfigPath}\nProcessor: {_processorConfigPath}\n請重新啟動應用程式以套用新設定。",
                "設定已儲存",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"儲存設定失敗: {ex.Message}",
                "儲存失敗",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void SyncProcessorSettings(DownloaderIniSettings settings)
    {
        var processorConfiguration = LoadProcessorConfigurationOrDefault();

        ProcessorIniFileWriter.Write(
            _processorConfigPath,
            processorConfiguration,
            settings.FolderAPath,
            settings.FolderBPath,
            settings.ProcessorFileNamingRule,
            settings.ProcessorKnowledgeRepositoryFile,
            settings.MofAccount,
            settings.OfficialApiAppId);
    }

    private ModuleConfiguration LoadProcessorConfigurationOrDefault()
    {
        try
        {
            if (File.Exists(_processorConfigPath))
            {
                var loader = new IniModuleConfigurationLoader();
                return loader.Load(_processorConfigPath);
            }
        }
        catch
        {
            // Fallback to defaults so settings can still be saved.
        }

        return new ModuleConfiguration(
            ModuleName: "InvoiceAiProcessor",
            FolderAPath: "data\\folder-a",
            FolderBPath: "data\\folder-b",
            OutputNamingRule: "{taxId}_{companyName}_{dateFrom}_{dateTo}.xlsx",
            MofAccount: "replace-with-mof-account",
            MofCredentialKey: "replace-with-official-app-key",
            KnowledgeRepositoryPath: "data\\knowledge\\knowledge-cases.json",
            OfficialApiAppId: "replace-with-official-app-id",
            OfficialApiAppKey: "replace-with-official-app-key");
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
        if (!string.IsNullOrWhiteSpace(_configuration?.OfficialApiAppKey))
        {
            return _configuration.OfficialApiAppKey;
        }

        return _configuration?.OfficialApiAppId ?? string.Empty;
    }

    private string GetEffectiveOfficialApiAppId()
    {
        if (!string.IsNullOrWhiteSpace(_configuration?.OfficialApiAppId))
        {
            return _configuration.OfficialApiAppId;
        }

        return _configuration?.OfficialApiAppKey ?? string.Empty;
    }

    private string ResolveConfiguredPath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        return Path.GetFullPath(Path.Combine(_solutionRootPath, configuredPath));
    }

    private DownloaderIniSettings BuildSettingsFromCurrentState()
    {
        var processorConfiguration = LoadProcessorConfigurationOrDefault();

        if (_configuration is null)
        {
            return new DownloaderIniSettings(
                ModuleName: "InvoiceDownloader",
                FolderAPath: string.Empty,
                FolderBPath: string.Empty,
                FileNamingRule: "{taxId}_{companyName}_{timestamp}.xlsx",
                ProcessorFileNamingRule: processorConfiguration.OutputNamingRule,
                ProcessorKnowledgeRepositoryFile: processorConfiguration.KnowledgeRepositoryPath,
                MofAccount: string.Empty,
                InvoiceDownloadMode: "local",
                OfficialApiBaseUrl: "https://service-mc.einvoice.nat.gov.tw/ptl/portal/api",
                OfficialApiAppId: string.Empty,
                OfficialApiAppKey: string.Empty);
        }

        return new DownloaderIniSettings(
            ModuleName: _configuration.ModuleName,
            FolderAPath: ResolveConfiguredPath(_configuration.FolderAPath),
            FolderBPath: ResolveConfiguredPath(_configuration.FolderBPath),
            FileNamingRule: _configuration.OutputNamingRule,
            ProcessorFileNamingRule: processorConfiguration.OutputNamingRule,
            ProcessorKnowledgeRepositoryFile: processorConfiguration.KnowledgeRepositoryPath,
            MofAccount: _configuration.MofAccount,
            InvoiceDownloadMode: _configuration.InvoiceDownloadMode,
            OfficialApiBaseUrl: _configuration.OfficialApiBaseUrl ?? string.Empty,
            OfficialApiAppId: _configuration.OfficialApiAppId ?? string.Empty,
            OfficialApiAppKey: _configuration.OfficialApiAppKey ?? string.Empty);
    }

    private static (IBatchInvoiceDownloader Downloader, HttpClient? OfficialApiHttpClient) CreateBatchDownloader(ModuleConfiguration configuration)
    {
        if (!string.Equals(configuration.InvoiceDownloadMode, "official", StringComparison.OrdinalIgnoreCase))
        {
            return (new LocalBatchInvoiceDownloader(), null);
        }

        if (string.IsNullOrWhiteSpace(configuration.OfficialApiBaseUrl))
        {
            throw new InvalidOperationException("官方下載模式缺少 officialApiBaseUrl 設定。請更新 INI 後重啟。");
        }

        var httpClient = new HttpClient();
        var apiClient = new MofOfficialHttpInvoiceApiClient(httpClient, configuration.OfficialApiBaseUrl);
        var officialDownloader = new OfficialInvoiceBatchDownloader(apiClient);
        var exporter = new OfficialInvoiceCsvExporter();
        var batchDownloader = new OfficialBatchInvoiceDownloader(
            officialDownloader,
            exporter,
            includeSalesInvoices: true,
            includePurchaseInvoices: false);

        return (batchDownloader, httpClient);
    }

    private string GetDownloadModeLabel()
    {
        return string.Equals(_configuration?.InvoiceDownloadMode, "official", StringComparison.OrdinalIgnoreCase)
            ? "官方 API"
            : "本地模擬";
    }
}