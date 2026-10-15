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
    private readonly IBatchInvoiceDownloader _batchInvoiceDownloader;
    private readonly HttpClient? _officialApiHttpClient;
    private IReadOnlyList<CustomerRecord> _loadedCustomers = [];
    private string? _sessionToken;
    private DateTimeOffset? _sessionExpiresAt;
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

    private async void StartBatchDownloadButton_Click(object sender, RoutedEventArgs e)
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

        StartBatchDownloadButton.IsEnabled = false;

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

            var result = await _batchInvoiceDownloader.DownloadAsync(request);

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
            includePurchaseInvoices: true,
            includeSalesAllowances: true,
            includePurchaseAllowances: true);

        return (batchDownloader, httpClient);
    }

    private string GetDownloadModeLabel()
    {
        return string.Equals(_configuration?.InvoiceDownloadMode, "official", StringComparison.OrdinalIgnoreCase)
            ? "官方 API"
            : "本地模擬";
    }
}