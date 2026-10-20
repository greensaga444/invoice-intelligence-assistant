using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Windows;
using InvoiceIntelligence.Core.Configuration;

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
        InvoiceFileSuffixTextBox.Text = settings.InvoiceFileSuffix;
        AllowanceFileSuffixTextBox.Text = settings.AllowanceFileSuffix;
        InvoiceOutputLabelTextBox.Text = settings.InvoiceOutputLabel;
        AllowanceOutputLabelTextBox.Text = settings.AllowanceOutputLabel;
        ProcessorKnowledgeRepositoryFileTextBox.Text = settings.ProcessorKnowledgeRepositoryFile;
        LowConfidenceThresholdTextBox.Text = settings.LowConfidenceThreshold.ToString("0.00", CultureInfo.InvariantCulture);
        MofAccountTextBox.Text = settings.MofAccount;
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
        var invoiceFileSuffix = InvoiceFileSuffixTextBox.Text.Trim();
        var allowanceFileSuffix = AllowanceFileSuffixTextBox.Text.Trim();
        var invoiceOutputLabel = InvoiceOutputLabelTextBox.Text.Trim();
        var allowanceOutputLabel = AllowanceOutputLabelTextBox.Text.Trim();
        var processorKnowledgeRepositoryFile = ProcessorKnowledgeRepositoryFileTextBox.Text.Trim();
        var lowConfidenceThresholdText = LowConfidenceThresholdTextBox.Text.Trim();
        var mofAccount = MofAccountTextBox.Text.Trim();
        var officialApiBaseUrl = OfficialApiBaseUrlTextBox.Text.Trim();
        var officialApiAppId = OfficialApiAppIdTextBox.Text.Trim();
        var officialApiAppKey = OfficialApiAppKeyTextBox.Text.Trim();
        var officialApiPassword = OfficialApiPasswordBox.Password;

        var validationInput = new LauncherSettingsInput(
            ModuleName: moduleName,
            FolderAPath: folderAPath,
            FolderBPath: folderBPath,
            FolderCPath: folderCPath,
            CustomerListFilePath: customerListFilePath,
            FileNamingRule: fileNamingRule,
            InvoiceFileSuffix: invoiceFileSuffix,
            AllowanceFileSuffix: allowanceFileSuffix,
            InvoiceOutputLabel: invoiceOutputLabel,
            AllowanceOutputLabel: allowanceOutputLabel,
            ProcessorKnowledgeRepositoryFile: processorKnowledgeRepositoryFile,
            LowConfidenceThresholdText: lowConfidenceThresholdText,
            MofAccount: mofAccount,
            OfficialApiBaseUrl: officialApiBaseUrl,
            OfficialApiAppId: officialApiAppId,
            OfficialApiAppKey: officialApiAppKey,
            OfficialApiPassword: officialApiPassword);

        if (!LauncherSettingsValidator.TryValidate(validationInput, out var errorMessage))
        {
            MessageBox.Show(this, errorMessage, "設定驗證", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _ = ConfigurationValueParser.TryParseLowConfidenceThreshold(lowConfidenceThresholdText, out var lowConfidenceThreshold);

        SavedSettings = new LauncherIniSettings(
            ModuleName: moduleName,
            FolderAPath: folderAPath,
            FolderBPath: folderBPath,
            FolderCPath: folderCPath,
            CustomerListFilePath: customerListFilePath,
            FileNamingRule: fileNamingRule,
            InvoiceFileSuffix: invoiceFileSuffix,
            AllowanceFileSuffix: allowanceFileSuffix,
            InvoiceOutputLabel: invoiceOutputLabel,
            AllowanceOutputLabel: allowanceOutputLabel,
            ProcessorKnowledgeRepositoryFile: processorKnowledgeRepositoryFile,
            LowConfidenceThreshold: lowConfidenceThreshold,
            MofAccount: mofAccount,
            OfficialApiBaseUrl: officialApiBaseUrl,
            OfficialApiAppId: officialApiAppId,
            OfficialApiAppKey: officialApiAppKey,
            OfficialApiPassword: officialApiPassword);

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

}
