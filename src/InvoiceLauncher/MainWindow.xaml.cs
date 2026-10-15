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
    private readonly string _downloaderExecutablePath;
    private readonly string _processorExecutablePath;
    private readonly string _downloaderConfigPath;
    private readonly string _processorConfigPath;

    public MainWindow()
    {
        InitializeComponent();

        _packageRootPath = ResolvePackageRootPath();
        _configFolderPath = Path.Combine(_packageRootPath, "config");
        _downloaderFolderPath = Path.GetFullPath(Path.Combine(_packageRootPath, "downloader"));
        _processorFolderPath = Path.GetFullPath(Path.Combine(_packageRootPath, "processor"));
        _downloaderExecutablePath = Path.Combine(_downloaderFolderPath, "InvoiceDownloader.exe");
        _processorExecutablePath = Path.Combine(_processorFolderPath, "InvoiceAiProcessor.exe");
        _downloaderConfigPath = Path.Combine(_configFolderPath, "downloader.ini");
        _processorConfigPath = Path.Combine(_configFolderPath, "processor.ini");

        RefreshModuleStatus();
        AppendLog($"主控台已啟動，根目錄：{_packageRootPath}");
    }

    private async void RunFullFlowButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureModuleReady(_downloaderExecutablePath, _downloaderConfigPath, "Downloader")
            || !EnsureModuleReady(_processorExecutablePath, _processorConfigPath, "Processor"))
        {
            return;
        }

        SetActionButtonsEnabled(false);

        try
        {
            AppendLog("開始完整流程：啟動 Downloader。");
            var downloaderExitCode = await StartAndWaitForExitAsync(
                _downloaderExecutablePath,
                _downloaderFolderPath);

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
            var processorExitCode = await StartAndWaitForExitAsync(
                _processorExecutablePath,
                _processorFolderPath);
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
        LaunchModuleWithoutWaiting(_downloaderExecutablePath, _downloaderFolderPath, _downloaderConfigPath, "Downloader");
    }

    private void LaunchProcessorButton_Click(object sender, RoutedEventArgs e)
    {
        LaunchModuleWithoutWaiting(_processorExecutablePath, _processorFolderPath, _processorConfigPath, "Processor");
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
            var processorConfiguration = LoadConfigurationOrDefault(_processorConfigPath, CreateDefaultProcessorConfig());

            DownloaderIniFileWriter.Write(_downloaderConfigPath, settingsWindow.SavedSettings);
            ProcessorIniFileWriter.Write(_processorConfigPath, processorConfiguration, settingsWindow.SavedSettings);

            AppendLog("設定已更新至根目錄 config。啟動中的模組請重啟後套用。");
            MessageBox.Show(
                this,
                $"設定已儲存。\nDownloader: {_downloaderConfigPath}\nProcessor: {_processorConfigPath}",
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
        OpenFolder(_downloaderFolderPath);
    }

    private void OpenProcessorFolderButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFolder(_processorFolderPath);
    }

    private static async Task<int> StartAndWaitForExitAsync(string executablePath, string workingDirectory)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = workingDirectory,
            UseShellExecute = true
        });

        if (process is null)
        {
            throw new InvalidOperationException($"無法啟動程序：{executablePath}");
        }

        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    private void LaunchModuleWithoutWaiting(
        string executablePath,
        string workingDirectory,
        string configPath,
        string moduleDisplayName)
    {
        if (!EnsureModuleReady(executablePath, configPath, moduleDisplayName))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                WorkingDirectory = workingDirectory,
                UseShellExecute = true
            });

            AppendLog($"{moduleDisplayName} 已啟動。");
        }
        catch (Exception ex)
        {
            AppendLog($"啟動 {moduleDisplayName} 失敗：{ex.Message}");
            MessageBox.Show(this, ex.Message, "啟動失敗", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool EnsureModuleReady(string executablePath, string configPath, string moduleDisplayName)
    {
        if (!File.Exists(executablePath))
        {
            var executableMessage = $"找不到 {moduleDisplayName} 可執行檔：{executablePath}";
            AppendLog(executableMessage);
            MessageBox.Show(this, executableMessage, "模組未就緒", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (File.Exists(configPath))
        {
            return true;
        }

        var configMessage = $"找不到 {moduleDisplayName} 設定檔：{configPath}";
        AppendLog(configMessage);
        MessageBox.Show(this, configMessage, "設定未就緒", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private void RefreshModuleStatus()
    {
        DownloaderPathText.Text = _downloaderExecutablePath;
        ProcessorPathText.Text = _processorExecutablePath;

        var downloaderReady = File.Exists(_downloaderExecutablePath);
        var processorReady = File.Exists(_processorExecutablePath);
        var downloaderConfigReady = File.Exists(_downloaderConfigPath);
        var processorConfigReady = File.Exists(_processorConfigPath);

        DownloaderStatusText.Text = downloaderReady
            ? downloaderConfigReady ? "已就緒" : "缺設定"
            : "未就緒";
        ProcessorStatusText.Text = processorReady
            ? processorConfigReady ? "已就緒" : "缺設定"
            : "未就緒";

        OpenDownloaderFolderButton.IsEnabled = Directory.Exists(_downloaderFolderPath);
        OpenProcessorFolderButton.IsEnabled = Directory.Exists(_processorFolderPath);
        RunFullFlowButton.IsEnabled = downloaderReady && processorReady && downloaderConfigReady && processorConfigReady;
        LaunchDownloaderButton.IsEnabled = downloaderReady && downloaderConfigReady;
        LaunchProcessorButton.IsEnabled = processorReady && processorConfigReady;
    }

    private void SetActionButtonsEnabled(bool isEnabled)
    {
        RunFullFlowButton.IsEnabled = isEnabled
            && File.Exists(_downloaderExecutablePath)
            && File.Exists(_processorExecutablePath)
            && File.Exists(_downloaderConfigPath)
            && File.Exists(_processorConfigPath);
        LaunchDownloaderButton.IsEnabled = isEnabled
            && File.Exists(_downloaderExecutablePath)
            && File.Exists(_downloaderConfigPath);
        LaunchProcessorButton.IsEnabled = isEnabled
            && File.Exists(_processorExecutablePath)
            && File.Exists(_processorConfigPath);
        RefreshStatusButton.IsEnabled = isEnabled;
        OpenSettingsButton.IsEnabled = isEnabled;
    }

    private void OpenFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            AppendLog($"資料夾不存在：{folderPath}");
            MessageBox.Show(this, $"資料夾不存在：{folderPath}", "無法開啟", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = folderPath,
            UseShellExecute = true
        });

        AppendLog($"已開啟資料夾：{folderPath}");
    }

    private void AppendLog(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        ExecutionLogTextBox.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
        ExecutionLogTextBox.ScrollToEnd();
    }

    private LauncherIniSettings BuildSettingsFromCurrentState()
    {
        var downloaderConfiguration = LoadConfigurationOrDefault(_downloaderConfigPath, CreateDefaultDownloaderConfig());
        var processorConfiguration = LoadConfigurationOrDefault(_processorConfigPath, CreateDefaultProcessorConfig());
        var officialApiPassword = ResolveOfficialApiPassword(downloaderConfiguration.OfficialApiPasswordEncrypted);

        return new LauncherIniSettings(
            ModuleName: downloaderConfiguration.ModuleName,
            FolderAPath: ResolveConfiguredPath(downloaderConfiguration.FolderAPath),
            FolderBPath: ResolveConfiguredPath(downloaderConfiguration.FolderBPath),
            FileNamingRule: downloaderConfiguration.OutputNamingRule,
            ProcessorFileNamingRule: processorConfiguration.OutputNamingRule,
            ProcessorKnowledgeRepositoryFile: ResolveConfiguredPath(processorConfiguration.KnowledgeRepositoryPath),
            MofAccount: downloaderConfiguration.MofAccount,
            InvoiceDownloadMode: downloaderConfiguration.InvoiceDownloadMode,
            OfficialApiBaseUrl: downloaderConfiguration.OfficialApiBaseUrl ?? string.Empty,
            OfficialApiAppId: downloaderConfiguration.OfficialApiAppId ?? string.Empty,
                OfficialApiAppKey: downloaderConfiguration.OfficialApiAppKey ?? string.Empty,
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
            KnowledgeRepositoryPath: "data\\knowledge\\knowledge-cases.json",
            InvoiceDownloadMode: "local",
            OfficialApiBaseUrl: "https://service-mc.einvoice.nat.gov.tw/ptl/portal/api",
            OfficialApiAppId: "replace-with-official-app-id",
            OfficialApiAppKey: "replace-with-official-app-key");
    }

    private ModuleConfiguration CreateDefaultProcessorConfig()
    {
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

    private string ResolveConfiguredPath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        return Path.GetFullPath(Path.Combine(_packageRootPath, configuredPath));
    }

    private static string ResolveOfficialApiPassword(string? encryptedPassword)
    {
        return MofPasswordProtector.TryDecrypt(encryptedPassword, out var plainPassword)
            ? plainPassword
            : string.Empty;
    }

    private static string ResolvePackageRootPath()
    {
        var downloaderConfigPath = ResolveConfigPath("downloader.ini");
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

    private static string ResolveConfigPath(string fileName)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "config", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "config", fileName);
    }
}