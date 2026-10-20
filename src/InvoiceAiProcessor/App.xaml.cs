using System.Windows;
using InvoiceIntelligence.Core.Configuration;

namespace InvoiceAiProcessor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var orchestrator = new ProcessorStartupOrchestrator();
        ProcessorInitialState startupState;

        try
        {
            startupState = orchestrator.BuildInitialState();
        }
        catch (Exception ex)
        {
            startupState = new ProcessorInitialState(
                StatusMessage: $"設定載入失敗: {ex.Message}",
                FolderAPath: string.Empty,
                FolderBPath: string.Empty,
                FolderCPath: string.Empty,
                OutputNamingRule: "{taxId}_{timestamp}.xlsx",
                InvoiceOutputLabel: ConfigurationDefaults.InvoiceOutputLabel,
                AllowanceOutputLabel: ConfigurationDefaults.AllowanceOutputLabel,
                InvoiceFileSuffix: ConfigurationDefaults.InvoiceFileSuffix,
                AllowanceFileSuffix: ConfigurationDefaults.AllowanceFileSuffix,
                LowConfidenceThreshold: ConfigurationDefaults.LowConfidenceThreshold,
                KnowledgeRepositoryPath: string.Empty,
                FeedbackService: null);
        }

        var mainWindow = new MainWindow(orchestrator, startupState);
        mainWindow.Show();
    }
}
