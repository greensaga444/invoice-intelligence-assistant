using System.IO;
using InvoiceIntelligence.Core.Configuration;
using InvoiceIntelligence.Core.Processor;

namespace InvoiceAiProcessor;

internal sealed class ProcessorStartupOrchestrator
{
    public ProcessorStartupState BuildStartupState()
    {
        var loader = new IniModuleConfigurationLoader();
        var configPath = PathResolutionHelper.ResolveConfigPath("processor.ini");
        var solutionRootPath = PathResolutionHelper.ResolveSolutionRootPath(configPath);
        var config = loader.Load(configPath);
        var folderAPath = PathResolutionHelper.ResolveConfiguredPath(solutionRootPath, config.FolderAPath);
        var folderBPath = PathResolutionHelper.ResolveConfiguredPath(solutionRootPath, config.FolderBPath);
        var folderCPath = PathResolutionHelper.ResolveConfiguredPath(solutionRootPath, config.FolderCPath);
        var knowledgeRepositoryPath = PathResolutionHelper.ResolveConfiguredPath(solutionRootPath, config.KnowledgeRepositoryPath);

        var pipelineState = BuildPipelineState(
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

        var message = $"載入成功: {config.ModuleName}\n" +
                      $"資料夾 A: {folderAPath}\n" +
                      $"資料夾 B: {folderBPath}\n" +
                      $"資料夾 C: {folderCPath}\n" +
                      $"人工覆核門檻: {config.LowConfidenceThreshold:0.00}\n" +
                      $"命名規則: {config.OutputNamingRule}\n" +
                      $"知識庫檔案: {knowledgeRepositoryPath}\n" +
                      pipelineState.StatusMessage;

        return pipelineState with { StatusMessage = message };
    }

    private static ProcessorStartupState BuildPipelineState(
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
            return new ProcessorStartupState(
                StatusMessage: "AI 決策流程：資料夾 A 尚無可分析檔案。",
                DecisionResults: [],
                FeedbackService: feedbackService);
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
        var folderBExportResult = exporter.Export(
            readyForFolderB,
            folderBPath,
            outputNamingRule,
            invoiceOutputLabel: invoiceOutputLabel,
            allowanceOutputLabel: allowanceOutputLabel,
            invoiceFileSuffix: invoiceFileSuffix,
            allowanceFileSuffix: allowanceFileSuffix);
        var folderCExportResult = exporter.Export(
            needManualReview,
            folderCPath,
            outputNamingRule,
            invoiceOutputLabel: invoiceOutputLabel,
            allowanceOutputLabel: allowanceOutputLabel,
            invoiceFileSuffix: invoiceFileSuffix,
            allowanceFileSuffix: allowanceFileSuffix);

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
            $"覆核規則：同一公司來源群組（同檔名基底，-{invoiceFileSuffix}/-{allowanceFileSuffix}/-allwance 視為同組）只要有任一筆信心度 < {lowConfidenceThreshold:0.00}，該群組檔案即匯出至資料夾 C。";

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

        return new ProcessorStartupState(
            statusMessage,
            pipelineResult.Items,
            feedbackService);
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
