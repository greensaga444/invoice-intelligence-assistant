using System.Collections.ObjectModel;
using System.Windows;
using InvoiceIntelligence.Core.Processor;

namespace InvoiceAiProcessor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const string ApprovedBy = "人工覆核";
    private readonly ManualKnowledgeFeedbackService? _feedbackService;
    private readonly ObservableCollection<FeedbackCandidateRow> _feedbackRows;

    public MainWindow(
        string statusMessage,
        IReadOnlyList<InvoiceDecisionResult> decisionResults,
        ManualKnowledgeFeedbackService? feedbackService)
    {
        InitializeComponent();
        StatusText.Text = statusMessage;

        _feedbackService = feedbackService;
        _feedbackRows = new ObservableCollection<FeedbackCandidateRow>(
            decisionResults.Select(FeedbackCandidateRow.Create));

        FeedbackGrid.ItemsSource = _feedbackRows;

        if (_feedbackRows.Count == 0)
        {
            UpdateKnowledgeButton.IsEnabled = false;
            FeedbackStatusText.Text = "目前沒有可供人工回饋的判定資料。";
            return;
        }

        if (_feedbackService is null)
        {
            UpdateKnowledgeButton.IsEnabled = false;
            FeedbackStatusText.Text = "知識庫服務尚未初始化，無法更新。";
            return;
        }

        FeedbackStatusText.Text = "請勾選已人工覆核核准的資料，再按「更新知識庫」。";
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

            FeedbackGrid.Items.Refresh();
        }
        catch (Exception ex)
        {
            FeedbackStatusText.Text = $"知識庫更新失敗：{ex.Message}";
        }
    }

    private sealed class FeedbackCandidateRow
    {
        private FeedbackCandidateRow(InvoiceDecisionResult sourceResult)
        {
            SourceResult = sourceResult;
            InvoiceNumber = sourceResult.Record.InvoiceNumber;
            CompanyName = sourceResult.Record.CompanyName;
            ItemName = sourceResult.Record.ItemName;
            DeductibleStatus = sourceResult.Decision.DeductibleStatus;
            AccountingCategory = sourceResult.Decision.AccountingCategory;
            ConfidenceScoreDisplay = sourceResult.Decision.ConfidenceScore.ToString("0.00");
            ResolvedByDisplay = sourceResult.ResolvedBy switch
            {
                DecisionStage.RuleEngine => "規則引擎",
                DecisionStage.CaseCache => "案例快取",
                DecisionStage.Llm => "本地 LLM",
                _ => "未知來源"
            };
        }

        public bool IsApproved { get; set; }

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
    }
}