using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using InvoiceIntelligence.Core.Processor;
using InvoiceIntelligence.Core.Trainer;
using Microsoft.Win32;

namespace InvoiceTrainer;

public partial class MainWindow : Window
{
    private const string ApprovedBy = "人工覆核";
    private readonly TrainingWorkflowService? _trainingWorkflowService;
    private readonly ManualKnowledgeFeedbackService? _feedbackService;
    private readonly ObservableCollection<FeedbackCandidateRow> _feedbackRows;
    private string? _selectedDatasetPath;

    public MainWindow(
        string statusMessage,
        TrainingWorkflowService? trainingWorkflowService,
        IReadOnlyList<InvoiceDecisionResult> decisionResults,
        ManualKnowledgeFeedbackService? feedbackService)
    {
        InitializeComponent();
        _trainingWorkflowService = trainingWorkflowService;
        _feedbackService = feedbackService;
        _feedbackRows = new ObservableCollection<FeedbackCandidateRow>(
            decisionResults.Select(FeedbackCandidateRow.Create));
        FeedbackGrid.ItemsSource = _feedbackRows;
        StatusText.Text = statusMessage;
        TrainingStatusText.Text = _trainingWorkflowService is null
            ? "訓練服務尚未初始化。"
            : "請先選擇 .xlsx 資料集，再依序執行驗證、訓練、評估與發布。";

        if (_feedbackRows.Count == 0)
        {
            UpdateKnowledgeButton.IsEnabled = false;
            FeedbackStatusText.Text = "目前沒有可供人工回饋的判定資料。";
        }
        else if (_feedbackService is null)
        {
            UpdateKnowledgeButton.IsEnabled = false;
            FeedbackStatusText.Text = "知識庫服務尚未初始化，無法更新。";
        }
        else
        {
            FeedbackStatusText.Text = "請勾選已人工覆核核准的資料，再按「更新知識庫」。";
        }
    }

    private void UpdateKnowledgeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_feedbackService is null)
        {
            FeedbackStatusText.Text = "知識庫服務尚未初始化，無法更新。";
            return;
        }

        var approvedRows = _feedbackRows
            .Where(item => item.IsApproved)
            .ToArray();

        if (approvedRows.Length == 0)
        {
            FeedbackStatusText.Text = "尚未勾選任何核准資料，知識庫未更新。";
            return;
        }

        var approvedResults = approvedRows
            .Select(item => item.SourceResult)
            .ToArray();

        try
        {
            var updateResult = _feedbackService.ApplyApprovedResults(approvedResults, ApprovedBy);
            FeedbackStatusText.Text =
                $"知識庫更新完成：新增 {updateResult.AddedCount} 筆，略過重複 {updateResult.SkippedCount} 筆。\n" +
                $"檔案位置：{updateResult.RepositoryFilePath}";

            foreach (var row in approvedRows)
            {
                row.IsApproved = false;
            }
        }
        catch (Exception ex)
        {
            FeedbackStatusText.Text = $"知識庫更新失敗：{ex.Message}";
        }
    }

    private void SelectDatasetButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "選擇訓練資料集",
            Filter = "Excel 工作簿 (*.xlsx)|*.xlsx",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        _selectedDatasetPath = dialog.FileName;
        DatasetPathTextBox.Text = _selectedDatasetPath;
        TrainingStatusText.Text = $"已選擇資料集：{_selectedDatasetPath}";
    }

    private void ValidateDatasetButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetTrainingInputs(requireDataset: true, out var datasetPath))
        {
            return;
        }

        try
        {
            var validation = _trainingWorkflowService!.ValidateDataset(datasetPath!);
            TrainingStatusText.Text =
                $"資料驗證完成：工作表 {validation.WorksheetName}，總列數 {validation.TotalDataRows}，有效 {validation.ValidRows}，無效 {validation.InvalidRows}。\n" +
                BuildValidationIssuesSummary(validation);
        }
        catch (Exception ex)
        {
            TrainingStatusText.Text = $"資料驗證失敗：{ex.Message}";
        }
    }

    private void TrainModelButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetTrainingInputs(requireDataset: true, out var datasetPath))
        {
            return;
        }

        try
        {
            var actor = ResolveActor();
            var result = _trainingWorkflowService!.Train(datasetPath!, actor);
            TrainingStatusText.Text =
                $"訓練完成：模型條目 {result.ModelEntryCount}，訓練集 {result.TrainingRecordCount}，測試集 {result.TestRecordCount}。\n" +
                $"staged model：{result.StagedModelPath}\n" +
                BuildValidationIssuesSummary(result.Validation);
        }
        catch (Exception ex)
        {
            TrainingStatusText.Text = $"訓練失敗：{ex.Message}";
        }
    }

    private void EvaluateModelButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetTrainingInputs(requireDataset: true, out var datasetPath))
        {
            return;
        }

        try
        {
            var result = _trainingWorkflowService!.Evaluate(datasetPath!);
            TrainingStatusText.Text =
                $"評估完成：測試筆數 {result.Metrics.TotalTestRecords}，覆蓋 {result.Metrics.CoveredRecords}（{result.Metrics.CoverageRate:P1}）。\n" +
                $"可抵稅準確率 {result.Metrics.DeductibleAccuracy:P1}，科目準確率 {result.Metrics.AccountingCategoryAccuracy:P1}，原因一致率 {result.Metrics.DeductionReasonAccuracy:P1}。\n" +
                $"評估報告：{result.EvaluationReportPath}";
        }
        catch (Exception ex)
        {
            TrainingStatusText.Text = $"評估失敗：{ex.Message}";
        }
    }

    private void PublishModelButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetTrainingInputs(requireDataset: false, out _))
        {
            return;
        }

        try
        {
            var actor = ResolveActor();
            var result = _trainingWorkflowService!.Publish(actor);
            TrainingStatusText.Text =
                $"發布完成：{result.PublishedModelPath}\n" +
                $"核准人：{result.ApprovedBy}，發布時間(UTC)：{result.PublishedAtUtc:yyyy-MM-dd HH:mm:ss}";
        }
        catch (Exception ex)
        {
            TrainingStatusText.Text = $"發布失敗：{ex.Message}";
        }
    }

    private bool TryGetTrainingInputs(bool requireDataset, out string? datasetPath)
    {
        datasetPath = _selectedDatasetPath;

        if (_trainingWorkflowService is null)
        {
            TrainingStatusText.Text = "訓練服務尚未初始化，無法執行。";
            return false;
        }

        if (!requireDataset)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(datasetPath))
        {
            TrainingStatusText.Text = "請先選擇資料集。";
            return false;
        }

        if (!File.Exists(datasetPath))
        {
            TrainingStatusText.Text = $"找不到資料集檔案：{datasetPath}";
            return false;
        }

        return true;
    }

    private static string ResolveActor()
    {
        return string.IsNullOrWhiteSpace(Environment.UserName)
            ? "system"
            : Environment.UserName.Trim();
    }

    private static string BuildValidationIssuesSummary(TrainingDatasetValidationResult validation)
    {
        if (validation.Issues.Count == 0)
        {
            return "資料品質檢查：未發現錯誤。";
        }

        var preview = validation.Issues.Take(5).ToArray();
        var suffix = validation.Issues.Count > preview.Length
            ? $"\n...其餘 {validation.Issues.Count - preview.Length} 筆錯誤請修正後重試。"
            : string.Empty;

        return $"資料品質檢查（前 {preview.Length} 筆）：\n- {string.Join("\n- ", preview)}{suffix}";
    }

    private sealed class FeedbackCandidateRow : INotifyPropertyChanged
    {
        private bool _isApproved;

        private FeedbackCandidateRow(InvoiceDecisionResult sourceResult)
        {
            SourceResult = sourceResult;
            InvoiceNumber = sourceResult.Record.InvoiceNumber;
            CompanyName = sourceResult.Record.CompanyName;
            ItemName = sourceResult.Record.ItemName;
            DeductibleStatus = sourceResult.Decision.DeductibleStatus;
            AccountingCategory = sourceResult.Decision.AccountingCategory;
            ConfidenceScoreDisplay = sourceResult.Decision.ConfidenceScore.ToString("0.00");
            ResolvedByDisplay = sourceResult.ResolvedBy.ToDisplayName();
        }

        public bool IsApproved
        {
            get => _isApproved;
            set
            {
                if (_isApproved == value)
                {
                    return;
                }

                _isApproved = value;
                OnPropertyChanged();
            }
        }

        public string InvoiceNumber { get; }

        public string CompanyName { get; }

        public string ItemName { get; }

        public string DeductibleStatus { get; }

        public string AccountingCategory { get; }

        public string ConfidenceScoreDisplay { get; }

        public string ResolvedByDisplay { get; }

        public InvoiceDecisionResult SourceResult { get; }

        public static FeedbackCandidateRow Create(InvoiceDecisionResult sourceResult)
        {
            return new FeedbackCandidateRow(sourceResult);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
