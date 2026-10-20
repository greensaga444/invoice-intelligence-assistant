using System.IO;
using InvoiceIntelligence.Core.Configuration;
using InvoiceIntelligence.Core.Processor;
using InvoiceIntelligence.Core.Trainer;

namespace InvoiceTrainer;

internal sealed class TrainerStartupOrchestrator
{
    public TrainerStartupState BuildStartupState()
    {
        var loader = new IniModuleConfigurationLoader();
        var configPath = PathResolutionHelper.ResolveConfigPath("processor.ini");
        var solutionRootPath = PathResolutionHelper.ResolveSolutionRootPath(configPath);
        var config = loader.Load(configPath);
        var folderAPath = PathResolutionHelper.ResolveConfiguredPath(solutionRootPath, config.FolderAPath);
        var folderBPath = PathResolutionHelper.ResolveConfiguredPath(solutionRootPath, config.FolderBPath);
        var folderCPath = PathResolutionHelper.ResolveConfiguredPath(solutionRootPath, config.FolderCPath);
        var knowledgeRepositoryPath = PathResolutionHelper.ResolveConfiguredPath(solutionRootPath, config.KnowledgeRepositoryPath);

        var decisionState = BuildDecisionState(
            folderAPath,
            folderBPath,
            folderCPath,
            config.OutputNamingRule,
            config.InvoiceOutputLabel,
            config.AllowanceOutputLabel,
            config.InvoiceFileSuffix,
            config.AllowanceFileSuffix,
            config.LowConfidenceThreshold,
            knowledgeRepositoryPath);

        var statusMessage = $"訓練服務初始化完成。\n" +
                            $"資料夾 A: {folderAPath}\n" +
                            $"資料夾 B: {folderBPath}\n" +
                            $"資料夾 C: {folderCPath}\n" +
                            $"知識庫檔案: {knowledgeRepositoryPath}\n" +
                            decisionState.StatusMessage;

        return decisionState with
        {
            StatusMessage = statusMessage,
            TrainingService = new TrainingWorkflowService(solutionRootPath)
        };
    }

    private static TrainerStartupState BuildDecisionState(
        string folderAPath,
        string folderBPath,
        string folderCPath,
        string outputNamingRule,
        string invoiceOutputLabel,
        string allowanceOutputLabel,
        string invoiceFileSuffix,
        string allowanceFileSuffix,
        decimal lowConfidenceThreshold,
        string knowledgeRepositoryPath)
    {
        var reader = new FolderAInvoiceRecordReader();
        var records = reader.ReadFromFolder(folderAPath, invoiceFileSuffix, allowanceFileSuffix);
        var knowledgeRepository = new JsonKnowledgeCaseRepository(knowledgeRepositoryPath);
        var feedbackService = new ManualKnowledgeFeedbackService(knowledgeRepository);

        if (records.Count == 0)
        {
            return new TrainerStartupState(
                StatusMessage: "手動知識回饋：資料夾 A 尚無可分析檔案。",
                DecisionResults: [],
                FeedbackService: feedbackService,
                TrainingService: null);
        }

        var approvedKnowledgeCases = knowledgeRepository.LoadCases();
        var ragKnowledgeCases = KnowledgeRuleCatalog.DefaultKnowledgeCases
            .Concat(approvedKnowledgeCases)
            .ToArray();

        var pipeline = new InvoiceDecisionPipeline(
            new KeywordRuleEngine(),
            new InMemoryCaseCache(KnowledgeRuleCatalog.DefaultCaseCache),
            new VectorRagRetriever(ragKnowledgeCases),
            new LocalHeuristicLlmAnalyzer());

        var pipelineResult = pipeline.Process(records);
        var (readyForFolderB, needManualReview) = SplitResultsByReviewThreshold(
            pipelineResult.Items,
            lowConfidenceThreshold,
            invoiceFileSuffix,
            allowanceFileSuffix);

        var exporter = new FolderBEnhancedInvoiceExporter();
        _ = exporter.Export(
            readyForFolderB,
            folderBPath,
            outputNamingRule,
            invoiceOutputLabel: invoiceOutputLabel,
            allowanceOutputLabel: allowanceOutputLabel,
            invoiceFileSuffix: invoiceFileSuffix,
            allowanceFileSuffix: allowanceFileSuffix);
        _ = exporter.Export(
            needManualReview,
            folderCPath,
            outputNamingRule,
            invoiceOutputLabel: invoiceOutputLabel,
            allowanceOutputLabel: allowanceOutputLabel,
            invoiceFileSuffix: invoiceFileSuffix,
            allowanceFileSuffix: allowanceFileSuffix);

        var statusMessage =
            $"手動知識回饋來源已更新：總筆數 {pipelineResult.Summary.TotalRecords}，規則 {pipelineResult.Summary.ResolvedByRuleEngine}，快取 {pipelineResult.Summary.ResolvedByCaseCache}，LLM {pipelineResult.Summary.ResolvedByLlm}。\n" +
            $"知識案例：內建 {KnowledgeRuleCatalog.DefaultKnowledgeCases.Count} 筆，已核准 {approvedKnowledgeCases.Count} 筆。";

        return new TrainerStartupState(
            statusMessage,
            pipelineResult.Items,
            feedbackService,
            null);
    }

    private static (IReadOnlyList<InvoiceDecisionResult> ReadyForFolderB, IReadOnlyList<InvoiceDecisionResult> NeedManualReview)
        SplitResultsByReviewThreshold(
            IReadOnlyCollection<InvoiceDecisionResult> decisionResults,
            decimal lowConfidenceThreshold,
            string invoiceFileSuffix,
            string allowanceFileSuffix)
    {
        var readyForFolderB = new List<InvoiceDecisionResult>(decisionResults.Count);
        var needManualReview = new List<InvoiceDecisionResult>();

        var groupedByReviewGroup = decisionResults
            .GroupBy(
                item => FolderAFileCategoryHelper.BuildGroupKey(
                    item.Record.SourceFileName,
                    invoiceFileSuffix,
                    allowanceFileSuffix),
                StringComparer.OrdinalIgnoreCase);

        foreach (var group in groupedByReviewGroup)
        {
            if (group.Any(item => item.Decision.ConfidenceScore < lowConfidenceThreshold))
            {
                needManualReview.AddRange(group);
                continue;
            }

            readyForFolderB.AddRange(group);
        }

        return (readyForFolderB, needManualReview);
    }
}

internal sealed record TrainerStartupState(
    string StatusMessage,
    IReadOnlyList<InvoiceDecisionResult> DecisionResults,
    ManualKnowledgeFeedbackService? FeedbackService,
    TrainingWorkflowService? TrainingService);
