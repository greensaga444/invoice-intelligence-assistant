using System.IO;
using InvoiceIntelligence.Core.Configuration;
using InvoiceIntelligence.Core.Processor;

namespace InvoiceAiProcessor;

internal sealed class ProcessorStartupOrchestrator
{
    public ProcessorInitialState BuildInitialState()
    {
        var loader = new IniModuleConfigurationLoader();
        var configPath = PathResolutionHelper.ResolveConfigPath("processor.ini");
        var solutionRootPath = PathResolutionHelper.ResolveSolutionRootPath(configPath);
        var config = loader.Load(configPath);
        var folderAPath = PathResolutionHelper.ResolveConfiguredPath(solutionRootPath, config.FolderAPath);
        var folderBPath = PathResolutionHelper.ResolveConfiguredPath(solutionRootPath, config.FolderBPath);
        var folderCPath = PathResolutionHelper.ResolveConfiguredPath(solutionRootPath, config.FolderCPath);
        var knowledgeRepositoryPath = PathResolutionHelper.ResolveConfiguredPath(solutionRootPath, config.KnowledgeRepositoryPath);
        var knowledgeRepository = new JsonKnowledgeCaseRepository(knowledgeRepositoryPath);
        var feedbackService = new ManualKnowledgeFeedbackService(knowledgeRepository);

        var message =
            $"載入成功: {config.ModuleName}\n" +
            $"資料夾 A: {folderAPath}\n" +
            $"資料夾 B: {folderBPath}\n" +
            $"資料夾 C: {folderCPath}\n" +
            $"人工覆核門檻: {config.LowConfidenceThreshold:0.00}\n" +
            $"命名規則: {config.OutputNamingRule}\n" +
            $"知識庫檔案: {knowledgeRepositoryPath}\n" +
            "尚未開始處理，請按下「Start」。";

        return new ProcessorInitialState(
            StatusMessage: message,
            FolderAPath: folderAPath,
            FolderBPath: folderBPath,
            FolderCPath: folderCPath,
            OutputNamingRule: config.OutputNamingRule,
            InvoiceOutputLabel: config.InvoiceOutputLabel,
            AllowanceOutputLabel: config.AllowanceOutputLabel,
            InvoiceFileSuffix: config.InvoiceFileSuffix,
            AllowanceFileSuffix: config.AllowanceFileSuffix,
            LowConfidenceThreshold: config.LowConfidenceThreshold,
            KnowledgeRepositoryPath: knowledgeRepositoryPath,
            FeedbackService: feedbackService);
    }

    public ProcessorStartupState BuildStartupState()
    {
        var initialState = BuildInitialState();
        var processingState = RunProcessing(initialState);
        var combinedMessage = $"{initialState.StatusMessage}\n{processingState.StatusMessage}";

        return new ProcessorStartupState(
            combinedMessage,
            processingState.DecisionResults,
            processingState.FeedbackService);
    }

    public ProcessorProcessingState RunProcessing(
        ProcessorInitialState initialState,
        Action<ProcessorProgressUpdate>? reportProgress = null)
    {
        ArgumentNullException.ThrowIfNull(initialState);

        var reader = new FolderAInvoiceRecordReader();
        var records = reader.ReadFromFolder(
            initialState.FolderAPath,
            initialState.InvoiceFileSuffix,
            initialState.AllowanceFileSuffix);

        if (records.Count == 0)
        {
            reportProgress?.Invoke(new ProcessorProgressUpdate(0, 0, 100m));
            return new ProcessorProcessingState(
                StatusMessage: "AI 決策流程：資料夾 A 尚無可分析檔案。",
                DecisionResults: [],
                FeedbackService: initialState.FeedbackService,
                ProcessedRecords: 0,
                TotalRecords: 0,
                FolderBRecordCount: 0,
                FolderCRecordCount: 0,
                FolderBFileCount: 0,
                FolderCFileCount: 0);
        }

        reportProgress?.Invoke(new ProcessorProgressUpdate(0, records.Count, 0m));

        var knowledgeRepository = new JsonKnowledgeCaseRepository(initialState.KnowledgeRepositoryPath);
        var approvedKnowledgeCases = knowledgeRepository.LoadCases();
        var ragKnowledgeCases = KnowledgeRuleCatalog.DefaultKnowledgeCases
            .Concat(approvedKnowledgeCases)
            .ToArray();

        var pipeline = new InvoiceDecisionPipeline(
            new KeywordRuleEngine(),
            new InMemoryCaseCache(KnowledgeRuleCatalog.DefaultCaseCache),
            new VectorRagRetriever(ragKnowledgeCases),
            new LocalHeuristicLlmAnalyzer());

        var pipelineResult = pipeline.Process(records, (processed, total) =>
        {
            reportProgress?.Invoke(new ProcessorProgressUpdate(
                processed,
                total,
                CalculatePercentage(processed, total)));
        });
        var (readyForFolderB, needManualReview) = SplitResultsByReviewThreshold(
            pipelineResult.Items,
            initialState.LowConfidenceThreshold,
            initialState.InvoiceFileSuffix,
            initialState.AllowanceFileSuffix);

        var exporter = new FolderBEnhancedInvoiceExporter();
        var folderBExportResult = exporter.Export(
            readyForFolderB,
            initialState.FolderBPath,
            initialState.OutputNamingRule,
            invoiceOutputLabel: initialState.InvoiceOutputLabel,
            allowanceOutputLabel: initialState.AllowanceOutputLabel,
            invoiceFileSuffix: initialState.InvoiceFileSuffix,
            allowanceFileSuffix: initialState.AllowanceFileSuffix);
        var folderCExportResult = exporter.Export(
            needManualReview,
            initialState.FolderCPath,
            initialState.OutputNamingRule,
            invoiceOutputLabel: initialState.InvoiceOutputLabel,
            allowanceOutputLabel: initialState.AllowanceOutputLabel,
            invoiceFileSuffix: initialState.InvoiceFileSuffix,
            allowanceFileSuffix: initialState.AllowanceFileSuffix);

        var previewLines = pipelineResult.Items
            .Take(3)
            .Select(item =>
                $"- {item.Record.InvoiceNumber}: {item.Decision.DeductibleStatus}/{item.Decision.AccountingCategory} ({item.Decision.ConfidenceScore:0.00})（來源：{item.ResolvedBy.ToDisplayName()}）")
            .ToArray();

        var folderBFilesPreview = folderBExportResult.Files
            .Take(3)
            .Select(file =>
                $"- {Path.GetFileName(file.OutputFilePath)} ({file.RecordCount} 筆)")
            .ToArray();

        var folderCFilesPreview = folderCExportResult.Files
            .Take(3)
            .Select(file =>
                $"- {Path.GetFileName(file.OutputFilePath)} ({file.RecordCount} 筆)")
            .ToArray();

        var exportSummary =
            $"資料夾 B 匯出完成：檔案 {folderBExportResult.ExportedFileCount}，筆數 {folderBExportResult.ExportedRecordCount}。\n" +
            $"資料夾 C（人工覆核）匯出完成：檔案 {folderCExportResult.ExportedFileCount}，筆數 {folderCExportResult.ExportedRecordCount}。\n" +
            $"覆核規則：同一公司來源群組（同檔名基底，-{initialState.InvoiceFileSuffix}/-{initialState.AllowanceFileSuffix}/-allwance 視為同組）只要有任一筆信心度 < {initialState.LowConfidenceThreshold:0.00}，該群組檔案即匯出至資料夾 C。";

        if (folderBFilesPreview.Length > 0)
        {
            exportSummary += $"\n資料夾 B 輸出預覽:\n{string.Join("\n", folderBFilesPreview)}";
        }

        if (folderCFilesPreview.Length > 0)
        {
            exportSummary += $"\n資料夾 C 輸出預覽:\n{string.Join("\n", folderCFilesPreview)}\n" +
                             "請完成人工覆核後，將檔案複製回資料夾 B。";
        }

        var statusMessage =
            $"AI 決策流程完成：總筆數 {pipelineResult.Summary.TotalRecords}，規則 {pipelineResult.Summary.ResolvedByRuleEngine}，快取 {pipelineResult.Summary.ResolvedByCaseCache}，LLM {pipelineResult.Summary.ResolvedByLlm}。\n" +
            $"知識案例：內建 {KnowledgeRuleCatalog.DefaultKnowledgeCases.Count} 筆，已核准 {approvedKnowledgeCases.Count} 筆。\n" +
            $"判定預覽:\n{string.Join("\n", previewLines)}\n" +
            exportSummary;

        reportProgress?.Invoke(new ProcessorProgressUpdate(
            records.Count,
            records.Count,
            100m));

        return new ProcessorProcessingState(
            statusMessage,
            pipelineResult.Items,
            initialState.FeedbackService,
            ProcessedRecords: records.Count,
            TotalRecords: records.Count,
            FolderBRecordCount: folderBExportResult.ExportedRecordCount,
            FolderCRecordCount: folderCExportResult.ExportedRecordCount,
            FolderBFileCount: folderBExportResult.ExportedFileCount,
            FolderCFileCount: folderCExportResult.ExportedFileCount);
    }

    private static decimal CalculatePercentage(int processed, int total)
    {
        if (total <= 0)
        {
            return 100m;
        }

        return Math.Round(processed * 100m / total, 2, MidpointRounding.AwayFromZero);
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

internal sealed record ProcessorStartupState(
    string StatusMessage,
    IReadOnlyList<InvoiceDecisionResult> DecisionResults,
    ManualKnowledgeFeedbackService? FeedbackService);

internal sealed record ProcessorInitialState(
    string StatusMessage,
    string FolderAPath,
    string FolderBPath,
    string FolderCPath,
    string OutputNamingRule,
    string InvoiceOutputLabel,
    string AllowanceOutputLabel,
    string InvoiceFileSuffix,
    string AllowanceFileSuffix,
    decimal LowConfidenceThreshold,
    string KnowledgeRepositoryPath,
    ManualKnowledgeFeedbackService? FeedbackService);

internal sealed record ProcessorProcessingState(
    string StatusMessage,
    IReadOnlyList<InvoiceDecisionResult> DecisionResults,
    ManualKnowledgeFeedbackService? FeedbackService,
    int ProcessedRecords,
    int TotalRecords,
    int FolderBRecordCount,
    int FolderCRecordCount,
    int FolderBFileCount,
    int FolderCFileCount);

internal sealed record ProcessorProgressUpdate(
    int ProcessedRecords,
    int TotalRecords,
    decimal Percentage);
