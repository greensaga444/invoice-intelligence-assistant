using System.Windows;

namespace InvoiceAiProcessor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var openTrainingTab = e.Args.Any(arg => string.Equals(arg, "--training", StringComparison.OrdinalIgnoreCase));

        ProcessorStartupState startupState;

        try
        {
            startupState = new ProcessorStartupOrchestrator().BuildStartupState();
        }
        catch (Exception ex)
        {
            startupState = new ProcessorStartupState(
                StatusMessage: $"設定載入失敗: {ex.Message}",
                DecisionResults: [],
                FeedbackService: null,
                TrainingService: null);
        }

        var mainWindow = new MainWindow(
            startupState.StatusMessage,
            startupState.DecisionResults,
            startupState.FeedbackService,
            startupState.TrainingService,
            openTrainingTab);
        mainWindow.Show();
    }
}
