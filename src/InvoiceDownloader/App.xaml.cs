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

        try
        {
            var loader = new IniModuleConfigurationLoader();
            var configPath = ResolveConfigPath("downloader.ini");
            var config = loader.Load(configPath);

            message = $"載入成功: {config.ModuleName}\n" +
                      $"Folder A: {config.FolderAPath}\n" +
                      $"Folder B: {config.FolderBPath}\n" +
                      $"命名規則: {config.OutputNamingRule}";
        }
        catch (Exception ex)
        {
            message = $"設定載入失敗: {ex.Message}";
        }

        var mainWindow = new MainWindow(message);
        mainWindow.Show();
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

