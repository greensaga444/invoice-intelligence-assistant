using System.IO;
using System.Windows;
using InvoiceIntelligence.Core.Configuration;

namespace InvoiceDownloader;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        string message;
        ModuleConfiguration? config = null;

        try
        {
            var loader = new IniModuleConfigurationLoader();
            var configPath = ResolveConfigPath("downloader.ini");
            var solutionRootPath = ResolveSolutionRootPath(configPath);
            config = loader.Load(configPath);

            message = $"載入成功: {config.ModuleName}\n" +
                      $"Folder A: {config.FolderAPath}\n" +
                      $"Folder B: {config.FolderBPath}\n" +
                      $"命名規則: {config.OutputNamingRule}\n" +
                      "請先匯入客戶清單，再進行 MOF 驗證與批次下載。";

            var startupWindow = new MainWindow(config, message, solutionRootPath);
            startupWindow.Show();
            return;
        }
        catch (Exception ex)
        {
            message = $"設定載入失敗: {ex.Message}";
        }

        var fallbackWindow = new MainWindow(config, message, AppContext.BaseDirectory);
        fallbackWindow.Show();
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

    private static string ResolveSolutionRootPath(string configPath)
    {
        var configDirectory = Path.GetDirectoryName(configPath)
            ?? throw new InvalidOperationException("Cannot resolve config directory path.");

        var solutionRootDirectory = Directory.GetParent(configDirectory)?.FullName;
        return solutionRootDirectory ?? AppContext.BaseDirectory;
    }
}

