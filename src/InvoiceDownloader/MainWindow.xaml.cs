using System.IO;
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
    private readonly ICustomerListImporter _customerListImporter;
    private readonly IMofAuthenticator _mofAuthenticator;
    private IReadOnlyList<CustomerRecord> _loadedCustomers = [];

    public MainWindow(ModuleConfiguration? configuration, string statusMessage)
    {
        InitializeComponent();

        _configuration = configuration;
        _customerListImporter = new CustomerListExcelImporter();
        _mofAuthenticator = new LocalMofAuthenticator();

        StartupStatusText.Text = statusMessage;

        if (_configuration is null)
        {
            ImportCustomerFileButton.IsEnabled = false;
            AuthenticateButton.IsEnabled = false;
            return;
        }

        MofAccountValueText.Text = _configuration.MofAccount;
        MofApiKeyValueText.Text = MaskApiKey(_configuration.MofCredentialKey);
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

            ImportResultText.Text = $"匯入成功，共載入 {_loadedCustomers.Count} 筆客戶資料。";
        }
        catch (Exception ex)
        {
            _loadedCustomers = [];
            CustomerDataGrid.ItemsSource = null;
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
            _configuration.MofCredentialKey,
            MofPasswordBox.Password);

        var authResult = _mofAuthenticator.Authenticate(credential);
        if (!authResult.IsSuccess)
        {
            MofAuthResultText.Text = $"驗證失敗: {authResult.Message}";
            return;
        }

        var expiresAtText = authResult.ExpiresAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
        MofAuthResultText.Text = $"{authResult.Message} 工作階段有效至: {expiresAtText}";

        MofPasswordBox.Clear();
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
}