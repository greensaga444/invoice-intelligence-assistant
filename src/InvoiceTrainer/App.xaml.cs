using System.Windows;

namespace InvoiceTrainer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        TrainerStartupState startupState;

        try
        {
            startupState = new TrainerStartupOrchestrator().BuildStartupState();
        }
        catch (Exception ex)
        {
            startupState = new TrainerStartupState(
                StatusMessage: $"訓練服務初始化失敗：{ex.Message}",
                DecisionResults: [],
                FeedbackService: null,
                TrainingService: null);
        }

        var mainWindow = new MainWindow(
            startupState.StatusMessage,
            startupState.TrainingService,
            startupState.DecisionResults,
            startupState.FeedbackService);
        mainWindow.Show();
    }
}
