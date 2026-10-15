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
        var configPath = PathResolutionHelper.ResolveConfigPath("downloader.ini");
        var solutionRootPath = PathResolutionHelper.ResolveSolutionRootPath(configPath);

        try
        {
            var loader = new IniModuleConfigurationLoader();
            config = loader.Load(configPath);

            message = $"載入成功: {config.ModuleName}\n" +
                      $"資料夾 A: {config.FolderAPath}\n" +
                      $"資料夾 B: {config.FolderBPath}\n" +
                      $"命名規則: {config.OutputNamingRule}\n" +
                      "下載模式: 官方 API\n" +
                      "請先匯入客戶清單，再進行 MOF 驗證與批次下載。";

            var startupWindow = new MainWindow(config, message, solutionRootPath);
            startupWindow.Show();
            return;
        }
        catch (Exception ex)
        {
            message = $"設定載入失敗: {ex.Message}";
        }

        var fallbackWindow = new MainWindow(config, message, solutionRootPath);
        fallbackWindow.Show();
    }
}

