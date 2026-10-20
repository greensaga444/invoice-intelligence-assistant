using System.Diagnostics;
using System.IO;
using System.Windows;
using InvoiceIntelligence.Core.Configuration;

namespace InvoiceLauncher;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly string _packageRootPath;
    private readonly string _configFolderPath;
    private readonly string _downloaderFolderPath;
    private readonly string _processorFolderPath;
    private readonly string _trainerFolderPath;
    private readonly ModuleRuntimeDefinition _downloaderModule;
    private readonly ModuleRuntimeDefinition _processorModule;
    private readonly ModuleRuntimeDefinition _trainerModule;
    private readonly ModuleRuntimeCoordinator _runtimeCoordinator;

    public MainWindow()
    {
        InitializeComponent();

        _packageRootPath = ResolvePackageRootPath();
        _configFolderPath = Path.Combine(_packageRootPath, "config");
        _downloaderFolderPath = Path.GetFullPath(Path.Combine(_packageRootPath, "downloader"));
        _processorFolderPath = Path.GetFullPath(Path.Combine(_packageRootPath, "processor"));
        _trainerFolderPath = Path.GetFullPath(Path.Combine(_packageRootPath, "trainer"));
        _downloaderModule = new ModuleRuntimeDefinition(
            DisplayName: "Downloader",
            ExecutablePath: Path.Combine(_downloaderFolderPath, "InvoiceDownloader.exe"),
            WorkingDirectory: _downloaderFolderPath,
            ConfigPath: Path.Combine(_configFolderPath, "downloader.ini"));
        _processorModule = new ModuleRuntimeDefinition(
            DisplayName: "Processor",
            ExecutablePath: Path.Combine(_processorFolderPath, "InvoiceAiProcessor.exe"),
            WorkingDirectory: _processorFolderPath,
            ConfigPath: Path.Combine(_configFolderPath, "processor.ini"));
        _trainerModule = new ModuleRuntimeDefinition(
            DisplayName: "Trainer",
            ExecutablePath: Path.Combine(_trainerFolderPath, "InvoiceTrainer.exe"),
            WorkingDirectory: _trainerFolderPath,
            ConfigPath: Path.Combine(_configFolderPath, "processor.ini"));
        _runtimeCoordinator = new ModuleRuntimeCoordinator();

        RefreshModuleStatus();
        AppendLog($"主控台已啟動，根目錄：{_packageRootPath}");
    }

    private async void RunFullFlowButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_runtimeCoordinator.EnsureModuleReady(this, _downloaderModule, AppendLog)
            || !_runtimeCoordinator.EnsureModuleReady(this, _processorModule, AppendLog))
        {
            return;
        }

        SetActionButtonsEnabled(false);

        try
        {
            AppendLog("開始完整流程：啟動 Downloader。");
            var downloaderExitCode = await _runtimeCoordinator.StartAndWaitForExitAsync(_downloaderModule);

            AppendLog($"Downloader 已結束，退出碼：{downloaderExitCode}。");

            var continueProcessor = MessageBox.Show(
                this,
                "是否立即啟動 Processor？",
                "完整流程",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (continueProcessor != MessageBoxResult.Yes)
            {
                AppendLog("使用者取消啟動 Processor。");
                return;
            }

            AppendLog("啟動 Processor。");
            var processorExitCode = await _runtimeCoordinator.StartAndWaitForExitAsync(_processorModule);
            AppendLog($"Processor 已結束，退出碼：{processorExitCode}。");
        }
        catch (Exception ex)
        {
            AppendLog($"完整流程執行失敗：{ex.Message}");
            MessageBox.Show(this, ex.Message, "執行失敗", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetActionButtonsEnabled(true);
            RefreshModuleStatus();
        }
    }

    private void LaunchDownloaderButton_Click(object sender, RoutedEventArgs e)
    {
        _runtimeCoordinator.LaunchWithoutWaiting(this, _downloaderModule, AppendLog);
    }

    private void LaunchProcessorButton_Click(object sender, RoutedEventArgs e)
    {
        _runtimeCoordinator.LaunchWithoutWaiting(this, _processorModule, AppendLog);
    }

    private void LaunchTrainerButton_Click(object sender, RoutedEventArgs e)
    {
        _runtimeCoordinator.LaunchWithoutWaiting(this, _trainerModule, AppendLog);
    }

    private void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new LauncherSettingsWindow(BuildSettingsFromCurrentState())
        {
            Owner = this
        };

        if (settingsWindow.ShowDialog() != true || settingsWindow.SavedSettings is null)
        {
            return;
        }

        try
        {
            var processorConfiguration = LoadConfigurationOrDefault(_processorModule.ConfigPath, CreateDefaultProcessorConfig());

            DownloaderIniFileWriter.Write(_downloaderModule.ConfigPath, settingsWindow.SavedSettings);
            ProcessorIniFileWriter.Write(_processorModule.ConfigPath, processorConfiguration, settingsWindow.SavedSettings);

            AppendLog("設定已更新至根目錄 config。啟動中的模組請重啟後套用。");
            MessageBox.Show(
                this,
                $"設定已儲存。\nDownloader: {_downloaderModule.ConfigPath}\nProcessor: {_processorModule.ConfigPath}",
                "設定已儲存",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            RefreshModuleStatus();
        }
        catch (Exception ex)
        {
            AppendLog($"儲存設定失敗：{ex.Message}");
            MessageBox.Show(this, $"儲存設定失敗: {ex.Message}", "儲存失敗", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshStatusButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshModuleStatus();
        AppendLog("已重新整理模組狀態。");
    }

    private void OpenDownloaderFolderButton_Click(object sender, RoutedEventArgs e)
    {
        _runtimeCoordinator.OpenFolder(this, _downloaderFolderPath, AppendLog);
    }

    private void OpenProcessorFolderButton_Click(object sender, RoutedEventArgs e)
    {
        _runtimeCoordinator.OpenFolder(this, _processorFolderPath, AppendLog);
    }

    private void OpenTrainerFolderButton_Click(object sender, RoutedEventArgs e)
    {
        _runtimeCoordinator.OpenFolder(this, _trainerFolderPath, AppendLog);
    }

    private void RefreshModuleStatus()
    {
        DownloaderPathText.Text = _downloaderModule.ExecutablePath;
        ProcessorPathText.Text = _processorModule.ExecutablePath;
        TrainerPathText.Text = _trainerModule.ExecutablePath;

        var downloaderReady = File.Exists(_downloaderModule.ExecutablePath);
        var processorReady = File.Exists(_processorModule.ExecutablePath);
        var trainerReady = File.Exists(_trainerModule.ExecutablePath);
        var downloaderConfigReady = File.Exists(_downloaderModule.ConfigPath);
        var processorConfigReady = File.Exists(_processorModule.ConfigPath);

        DownloaderStatusText.Text = downloaderReady
            ? downloaderConfigReady ? "已就緒" : "缺設定"
            : "未就緒";
        ProcessorStatusText.Text = processorReady
            ? processorConfigReady ? "已就緒" : "缺設定"
            : "未就緒";
        TrainerStatusText.Text = trainerReady ? "已就緒" : "未就緒";

        OpenDownloaderFolderButton.IsEnabled = Directory.Exists(_downloaderFolderPath);
        OpenProcessorFolderButton.IsEnabled = Directory.Exists(_processorFolderPath);
        OpenTrainerFolderButton.IsEnabled = Directory.Exists(_trainerFolderPath);
        RunFullFlowButton.IsEnabled = downloaderReady && processorReady && downloaderConfigReady && processorConfigReady;
        LaunchDownloaderButton.IsEnabled = downloaderReady && downloaderConfigReady;
        LaunchProcessorButton.IsEnabled = processorReady && processorConfigReady;
        LaunchTrainerButton.IsEnabled = trainerReady;
    }

    private void SetActionButtonsEnabled(bool isEnabled)
    {
        RunFullFlowButton.IsEnabled = isEnabled
            && File.Exists(_downloaderModule.ExecutablePath)
            && File.Exists(_processorModule.ExecutablePath)
            && File.Exists(_downloaderModule.ConfigPath)
            && File.Exists(_processorModule.ConfigPath);
        LaunchDownloaderButton.IsEnabled = isEnabled
            && File.Exists(_downloaderModule.ExecutablePath)
            && File.Exists(_downloaderModule.ConfigPath);
        LaunchProcessorButton.IsEnabled = isEnabled
            && File.Exists(_processorModule.ExecutablePath)
            && File.Exists(_processorModule.ConfigPath);
        LaunchTrainerButton.IsEnabled = isEnabled
            && File.Exists(_trainerModule.ExecutablePath);
        RefreshStatusButton.IsEnabled = isEnabled;
        OpenSettingsButton.IsEnabled = isEnabled;
    }

    private void AppendLog(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        ExecutionLogTextBox.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
        ExecutionLogTextBox.ScrollToEnd();
    }

    private LauncherIniSettings BuildSettingsFromCurrentState()
    {
        var downloaderConfiguration = LoadConfigurationOrDefault(_downloaderModule.ConfigPath, CreateDefaultDownloaderConfig());
        var processorConfiguration = LoadConfigurationOrDefault(_processorModule.ConfigPath, CreateDefaultProcessorConfig());
        var officialApiPassword = ResolveOfficialApiPassword(downloaderConfiguration.OfficialApiPasswordEncrypted);

        return new LauncherIniSettings(
            ModuleName: downloaderConfiguration.ModuleName,
            FolderAPath: ResolveConfiguredPath(downloaderConfiguration.FolderAPath),
            FolderBPath: ResolveConfiguredPath(downloaderConfiguration.FolderBPath),
            FolderCPath: ResolveConfiguredPath(processorConfiguration.FolderCPath),
            CustomerListFilePath: ResolveConfiguredPath(downloaderConfiguration.CustomerListFilePath),
            FileNamingRule: downloaderConfiguration.OutputNamingRule,
            InvoiceFileSuffix: downloaderConfiguration.InvoiceFileSuffix,
            AllowanceFileSuffix: downloaderConfiguration.AllowanceFileSuffix,
            InvoiceOutputLabel: processorConfiguration.InvoiceOutputLabel,
            AllowanceOutputLabel: processorConfiguration.AllowanceOutputLabel,
            ProcessorFileNamingRule: processorConfiguration.OutputNamingRule,
            ProcessorKnowledgeRepositoryFile: ResolveConfiguredPath(processorConfiguration.KnowledgeRepositoryPath),
            LowConfidenceThreshold: processorConfiguration.LowConfidenceThreshold,
            MofAccount: downloaderConfiguration.MofAccount,
            OfficialApiBaseUrl: downloaderConfiguration.OfficialApiBaseUrl ?? string.Empty,
            OfficialApiAppId: MofApiCredentialResolver.ResolveAppId(
                downloaderConfiguration.OfficialApiAppId,
                downloaderConfiguration.OfficialApiAppKey),
            OfficialApiAppKey: MofApiCredentialResolver.ResolveAppKey(
                downloaderConfiguration.OfficialApiAppId,
                downloaderConfiguration.OfficialApiAppKey),
            OfficialApiPassword: officialApiPassword);
    }

    private ModuleConfiguration LoadConfigurationOrDefault(string configPath, ModuleConfiguration fallback)
    {
        try
        {
            if (File.Exists(configPath))
            {
                var loader = new IniModuleConfigurationLoader();
                return loader.Load(configPath);
            }
        }
        catch
        {
            // Keep fallback defaults if config parsing fails.
        }

        return fallback;
    }

    private ModuleConfiguration CreateDefaultDownloaderConfig()
    {
        return new ModuleConfiguration(
            ModuleName: "InvoiceDownloader",
            FolderAPath: "data\\folder-a",
            FolderBPath: "data\\folder-b",
            OutputNamingRule: "{taxId}_{companyName}_{timestamp}.xlsx",
            MofAccount: "replace-with-mof-account",
            MofCredentialKey: "replace-with-official-app-key",
            KnowledgeRepositoryPath: ConfigurationDefaults.KnowledgeRepositoryPath,
            InvoiceDownloadMode: ConfigurationDefaults.OfficialDownloadMode,
            OfficialApiBaseUrl: ConfigurationDefaults.OfficialApiBaseUrl,
            OfficialApiAppId: "replace-with-official-app-id",
            OfficialApiAppKey: "replace-with-official-app-key",
            CustomerListFilePath: ConfigurationDefaults.CustomerListFilePath,
            InvoiceFileSuffix: ConfigurationDefaults.InvoiceFileSuffix,
            AllowanceFileSuffix: ConfigurationDefaults.AllowanceFileSuffix);
    }

    private ModuleConfiguration CreateDefaultProcessorConfig()
    {
        return new ModuleConfiguration(
            ModuleName: "InvoiceAiProcessor",
            FolderAPath: "data\\folder-a",
            FolderBPath: "data\\folder-b",
            FolderCPath: ConfigurationDefaults.FolderCPath,
            OutputNamingRule: "{taxId}_{companyName}_{dateFrom}_{dateTo}.xlsx",
            MofAccount: string.Empty,
            MofCredentialKey: string.Empty,
            KnowledgeRepositoryPath: ConfigurationDefaults.KnowledgeRepositoryPath,
            LowConfidenceThreshold: ConfigurationDefaults.LowConfidenceThreshold,
            OfficialApiAppId: string.Empty,
            OfficialApiAppKey: string.Empty,
            InvoiceFileSuffix: ConfigurationDefaults.InvoiceFileSuffix,
            AllowanceFileSuffix: ConfigurationDefaults.AllowanceFileSuffix);
    }

    private string ResolveConfiguredPath(string configuredPath)
    {
        return PathResolutionHelper.ResolveConfiguredPath(_packageRootPath, configuredPath);
    }

    private static string ResolveOfficialApiPassword(string? encryptedPassword)
    {
        return MofPasswordProtector.TryDecrypt(encryptedPassword, out var plainPassword)
            ? plainPassword
            : string.Empty;
    }

    private static string ResolvePackageRootPath()
    {
        var downloaderConfigPath = PathResolutionHelper.ResolveConfigPath("downloader.ini");
        var configDirectory = Path.GetDirectoryName(downloaderConfigPath);
        if (string.IsNullOrWhiteSpace(configDirectory))
        {
            return AppContext.BaseDirectory;
        }

        var packageRootDirectory = Directory.GetParent(configDirectory)?.FullName;
        return string.IsNullOrWhiteSpace(packageRootDirectory)
            ? AppContext.BaseDirectory
            : packageRootDirectory;
    }
}