using Microsoft.Win32;
using System.Windows;

namespace InvoiceLauncher;

public partial class LauncherSettingsWindow : Window
{
    public LauncherSettingsWindow(LauncherIniSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        InitializeComponent();

        ModuleNameTextBox.Text = settings.ModuleName;
        FolderAPathTextBox.Text = settings.FolderAPath;
        FolderBPathTextBox.Text = settings.FolderBPath;
        FileNamingRuleTextBox.Text = settings.FileNamingRule;
        ProcessorFileNamingRuleTextBox.Text = settings.ProcessorFileNamingRule;
        ProcessorKnowledgeRepositoryFileTextBox.Text = settings.ProcessorKnowledgeRepositoryFile;
        MofAccountTextBox.Text = settings.MofAccount;
        InvoiceDownloadModeTextBox.Text = settings.InvoiceDownloadMode;
        OfficialApiBaseUrlTextBox.Text = settings.OfficialApiBaseUrl;
        OfficialApiAppCredentialTextBox.Text = string.IsNullOrWhiteSpace(settings.OfficialApiAppId)
            ? settings.OfficialApiAppKey
            : settings.OfficialApiAppId;
    }

    public LauncherIniSettings? SavedSettings { get; private set; }

    private void PickFolderAButton_Click(object sender, RoutedEventArgs e)
    {
        PickFolderFor(FolderAPathTextBox, "選擇 Folder A 路徑");
    }

    private void PickFolderBButton_Click(object sender, RoutedEventArgs e)
    {
        PickFolderFor(FolderBPathTextBox, "選擇 Folder B 路徑");
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var moduleName = ModuleNameTextBox.Text.Trim();
        var folderAPath = FolderAPathTextBox.Text.Trim();
        var folderBPath = FolderBPathTextBox.Text.Trim();
        var fileNamingRule = FileNamingRuleTextBox.Text.Trim();
        var processorFileNamingRule = ProcessorFileNamingRuleTextBox.Text.Trim();
        var processorKnowledgeRepositoryFile = ProcessorKnowledgeRepositoryFileTextBox.Text.Trim();
        var mofAccount = MofAccountTextBox.Text.Trim();
        var invoiceDownloadMode = InvoiceDownloadModeTextBox.Text.Trim();
        var officialApiBaseUrl = OfficialApiBaseUrlTextBox.Text.Trim();
        var officialApiAppCredential = OfficialApiAppCredentialTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(moduleName)
            || string.IsNullOrWhiteSpace(folderAPath)
            || string.IsNullOrWhiteSpace(folderBPath)
            || string.IsNullOrWhiteSpace(fileNamingRule)
            || string.IsNullOrWhiteSpace(processorFileNamingRule)
            || string.IsNullOrWhiteSpace(processorKnowledgeRepositoryFile)
            || string.IsNullOrWhiteSpace(mofAccount)
            || string.IsNullOrWhiteSpace(invoiceDownloadMode)
            || string.IsNullOrWhiteSpace(officialApiAppCredential))
        {
            MessageBox.Show(this, "請完整填寫所有欄位。", "設定驗證", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.Equals(invoiceDownloadMode, "official", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(officialApiBaseUrl))
        {
            MessageBox.Show(this, "official 模式必須填寫官方 API Base URL。", "設定驗證", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SavedSettings = new LauncherIniSettings(
            moduleName,
            folderAPath,
            folderBPath,
            fileNamingRule,
            processorFileNamingRule,
            processorKnowledgeRepositoryFile,
            mofAccount,
            invoiceDownloadMode,
            officialApiBaseUrl,
            officialApiAppCredential,
            officialApiAppCredential);

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
}
