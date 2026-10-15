using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Windows;

namespace InvoiceLauncher;

public partial class LauncherSettingsWindow : Window
{
    private readonly string _moduleName;

    public LauncherSettingsWindow(LauncherIniSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        InitializeComponent();

        _moduleName = settings.ModuleName;
        FolderAPathTextBox.Text = settings.FolderAPath;
        FolderBPathTextBox.Text = settings.FolderBPath;
        FolderCPathTextBox.Text = settings.FolderCPath;
        CustomerListFilePathTextBox.Text = settings.CustomerListFilePath;
        FileNamingRuleTextBox.Text = settings.FileNamingRule;
        ProcessorFileNamingRuleTextBox.Text = settings.ProcessorFileNamingRule;
        ProcessorKnowledgeRepositoryFileTextBox.Text = settings.ProcessorKnowledgeRepositoryFile;
        LowConfidenceThresholdTextBox.Text = settings.LowConfidenceThreshold.ToString("0.00", CultureInfo.InvariantCulture);
        MofAccountTextBox.Text = settings.MofAccount;
        InvoiceDownloadModeTextBox.Text = settings.InvoiceDownloadMode;
        OfficialApiBaseUrlTextBox.Text = settings.OfficialApiBaseUrl;
        OfficialApiAppIdTextBox.Text = string.IsNullOrWhiteSpace(settings.OfficialApiAppId)
            ? settings.OfficialApiAppKey
            : settings.OfficialApiAppId;
        OfficialApiAppKeyTextBox.Text = string.IsNullOrWhiteSpace(settings.OfficialApiAppKey)
            ? settings.OfficialApiAppId
            : settings.OfficialApiAppKey;
        OfficialApiPasswordBox.Password = settings.OfficialApiPassword;
    }

    public LauncherIniSettings? SavedSettings { get; private set; }

    private void PickFolderAButton_Click(object sender, RoutedEventArgs e)
    {
        PickFolderFor(FolderAPathTextBox, "選擇原始發票資料夾");
    }

    private void PickFolderBButton_Click(object sender, RoutedEventArgs e)
    {
        PickFolderFor(FolderBPathTextBox, "選擇 AI 分析結果資料夾");
    }

    private void PickFolderCButton_Click(object sender, RoutedEventArgs e)
    {
        PickFolderFor(FolderCPathTextBox, "選擇人工覆核資料夾");
    }

    private void PickCustomerListFileButton_Click(object sender, RoutedEventArgs e)
    {
        PickFileFor(CustomerListFilePathTextBox, "選擇客戶清單 Excel 檔案", "Excel 檔案 (*.xlsx)|*.xlsx");
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var moduleName = _moduleName;
        var folderAPath = FolderAPathTextBox.Text.Trim();
        var folderBPath = FolderBPathTextBox.Text.Trim();
        var folderCPath = FolderCPathTextBox.Text.Trim();
        var customerListFilePath = CustomerListFilePathTextBox.Text.Trim();
        var fileNamingRule = FileNamingRuleTextBox.Text.Trim();
        var processorFileNamingRule = ProcessorFileNamingRuleTextBox.Text.Trim();
        var processorKnowledgeRepositoryFile = ProcessorKnowledgeRepositoryFileTextBox.Text.Trim();
        var lowConfidenceThresholdText = LowConfidenceThresholdTextBox.Text.Trim();
        var mofAccount = MofAccountTextBox.Text.Trim();
        var invoiceDownloadMode = InvoiceDownloadModeTextBox.Text.Trim();
        var officialApiBaseUrl = OfficialApiBaseUrlTextBox.Text.Trim();
        var officialApiAppId = OfficialApiAppIdTextBox.Text.Trim();
        var officialApiAppKey = OfficialApiAppKeyTextBox.Text.Trim();
        var officialApiPassword = OfficialApiPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(moduleName)
            || string.IsNullOrWhiteSpace(folderAPath)
            || string.IsNullOrWhiteSpace(folderBPath)
            || string.IsNullOrWhiteSpace(folderCPath)
            || string.IsNullOrWhiteSpace(customerListFilePath)
            || string.IsNullOrWhiteSpace(fileNamingRule)
            || string.IsNullOrWhiteSpace(processorFileNamingRule)
            || string.IsNullOrWhiteSpace(processorKnowledgeRepositoryFile)
            || string.IsNullOrWhiteSpace(mofAccount)
            || string.IsNullOrWhiteSpace(invoiceDownloadMode)
            || string.IsNullOrWhiteSpace(officialApiAppId)
            || string.IsNullOrWhiteSpace(officialApiAppKey))
        {
            MessageBox.Show(this, "請完整填寫所有欄位。", "設定驗證", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseLowConfidenceThreshold(lowConfidenceThresholdText, out var lowConfidenceThreshold))
        {
            MessageBox.Show(this, "低信心門檻必須為 0 到 1 之間的小數。例：0.80", "設定驗證", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.Equals(invoiceDownloadMode, "official", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(officialApiBaseUrl))
        {
            MessageBox.Show(this, "official 模式必須填寫官方 API Base URL。", "設定驗證", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.Equals(invoiceDownloadMode, "official", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(officialApiPassword))
        {
            MessageBox.Show(this, "official 模式必須填寫 MOF 密碼。", "設定驗證", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SavedSettings = new LauncherIniSettings(
            moduleName,
            folderAPath,
            folderBPath,
            folderCPath,
            customerListFilePath,
            fileNamingRule,
            processorFileNamingRule,
            processorKnowledgeRepositoryFile,
            lowConfidenceThreshold,
            mofAccount,
            invoiceDownloadMode,
            officialApiBaseUrl,
            officialApiAppId,
            officialApiAppKey,
            officialApiPassword);

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void PickFolderFor(System.Windows.Controls.TextBox targetTextBox, string title)
    {
        var dialog = new OpenFolderDialog
        {
            Multiselect = false,
            Title = title,
            FolderName = targetTextBox.Text
        };

        if (dialog.ShowDialog() == true)
        {
            targetTextBox.Text = dialog.FolderName;
        }
    }

    private static void PickFileFor(System.Windows.Controls.TextBox targetTextBox, string title, string filter)
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = false,
            CheckFileExists = true,
            Title = title,
            Filter = filter
        };

        var configuredPath = targetTextBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            if (Directory.Exists(configuredPath))
            {
                dialog.InitialDirectory = configuredPath;
            }
            else
            {
                var directoryPath = Path.GetDirectoryName(configuredPath);
                if (!string.IsNullOrWhiteSpace(directoryPath) && Directory.Exists(directoryPath))
                {
                    dialog.InitialDirectory = directoryPath;
                    dialog.FileName = Path.GetFileName(configuredPath);
                }
            }
        }

        if (dialog.ShowDialog() == true)
        {
            targetTextBox.Text = dialog.FileName;
        }
    }

    private static bool TryParseLowConfidenceThreshold(string thresholdText, out decimal threshold)
    {
        threshold = 0m;

        if (string.IsNullOrWhiteSpace(thresholdText))
        {
            return false;
        }

        var normalized = thresholdText.Trim();
        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out threshold)
            && !decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.CurrentCulture, out threshold))
        {
            return false;
        }

        if (threshold is < 0m or > 1m)
        {
            return false;
        }

        threshold = decimal.Round(threshold, 2, MidpointRounding.AwayFromZero);
        return true;
    }
}
