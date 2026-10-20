using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace InvoiceAiProcessor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ProcessorStartupOrchestrator _orchestrator;
    private readonly ProcessorInitialState _initialState;

    internal MainWindow(
        ProcessorStartupOrchestrator orchestrator,
        ProcessorInitialState initialState)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _initialState = initialState ?? throw new ArgumentNullException(nameof(initialState));

        InitializeComponent();
        StatusText.Text = initialState.StatusMessage;
        OpenFolderCButton.IsEnabled = !string.IsNullOrWhiteSpace(initialState.FolderCPath);
    }

    private async void StartButton_OnClick(object sender, RoutedEventArgs e)
    {
        StartButton.IsEnabled = false;
        ResultSummaryText.Text = string.Empty;
        ProcessingProgressBar.Value = 0;
        ProgressText.Text = "處理中：0/0 (0.00%)";

        IProgress<ProcessorProgressUpdate> progress = new Progress<ProcessorProgressUpdate>(update =>
        {
            ProgressText.Text = $"處理中：{update.ProcessedRecords}/{update.TotalRecords} ({update.Percentage:0.00}%)";
            ProcessingProgressBar.Value = (double)update.Percentage;
        });

        try
        {
            var processingState = await Task.Run(() =>
                _orchestrator.RunProcessing(
                    _initialState,
                    update => progress.Report(update)));

            StatusText.Text = $"{_initialState.StatusMessage}\n{processingState.StatusMessage}";
            ProgressText.Text = $"完成：{processingState.ProcessedRecords}/{processingState.TotalRecords} (100.00%)";
            ProcessingProgressBar.Value = 100;
            ResultSummaryText.Text =
                $"資料夾 B 總計：{processingState.FolderBRecordCount} 筆，{processingState.FolderBFileCount} 檔。\n" +
                $"資料夾 C 總計：{processingState.FolderCRecordCount} 筆，{processingState.FolderCFileCount} 檔。";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"執行失敗: {ex.Message}";
            ProgressText.Text = "處理失敗";
            ProcessingProgressBar.Value = 0;
            ResultSummaryText.Text = "請檢查設定與來源資料後重試。";
        }
        finally
        {
            StartButton.IsEnabled = true;
        }
    }

    private void OpenFolderCButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_initialState.FolderCPath))
        {
            MessageBox.Show("資料夾 C 路徑未設定。", "Open FolderC", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Directory.CreateDirectory(_initialState.FolderCPath);

        Process.Start(new ProcessStartInfo
        {
            FileName = _initialState.FolderCPath,
            UseShellExecute = true
        });
    }
}