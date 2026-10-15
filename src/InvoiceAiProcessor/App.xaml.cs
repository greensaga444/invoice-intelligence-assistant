using System.IO;
using System.Windows;
using InvoiceIntelligence.Core.Configuration;
using InvoiceIntelligence.Core.Processor;

namespace InvoiceAiProcessor;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ProcessorStartupState startupState;

        try
        {
            var loader = new IniModuleConfigurationLoader();
            var configPath = ResolveConfigPath("processor.ini");
            var solutionRootPath = ResolveSolutionRootPath(configPath);
            var config = loader.Load(configPath);
            var folderAPath = ResolveConfiguredPath(solutionRootPath, config.FolderAPath);
            var folderBPath = ResolveConfiguredPath(solutionRootPath, config.FolderBPath);
            var folderCPath = ResolveConfiguredPath(solutionRootPath, config.FolderCPath);
            var knowledgeRepositoryPath = ResolveConfiguredPath(solutionRootPath, config.KnowledgeRepositoryPath);

            var pipelineState = BuildPipelineState(
                folderAPath,
                folderBPath,
                folderCPath,
                config.OutputNamingRule,
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

            startupState = pipelineState with { StatusMessage = message };
        }
        catch (Exception ex)
        {
            startupState = new ProcessorStartupState(
                StatusMessage: $"設定載入失敗: {ex.Message}",
                DecisionResults: [],
                FeedbackService: null);
        }

        var mainWindow = new MainWindow(
            startupState.StatusMessage,
            startupState.DecisionResults,
            startupState.FeedbackService);
        mainWindow.Show();
    }

    private static ProcessorStartupState BuildPipelineState(
        string folderAPath,
        string folderBPath,
        string folderCPath,
        string outputNamingRule,
        decimal lowConfidenceThreshold,
        string knowledgeRepositoryPath)
    {
        var reader = new FolderAInvoiceRecordReader();
        var records = reader.ReadFromFolder(folderAPath);
        var knowledgeRepository = new JsonKnowledgeCaseRepository(knowledgeRepositoryPath);
        var feedbackService = new ManualKnowledgeFeedbackService(knowledgeRepository);

        if (records.Count == 0)
        {
            return new ProcessorStartupState(
                StatusMessage: "AI 決策流程：資料夾 A 尚無可分析檔案。",
                DecisionResults: [],
                FeedbackService: feedbackService);
        }

        var defaultKnowledgeCases = CreateDefaultKnowledgeCases();
        var approvedKnowledgeCases = knowledgeRepository.LoadCases();
        var ragKnowledgeCases = defaultKnowledgeCases
            .Concat(approvedKnowledgeCases)
            .ToArray();

        var pipeline = new InvoiceDecisionPipeline(
            new KeywordRuleEngine(),
            new InMemoryCaseCache(CreateDefaultCases()),
            new KeywordRagRetriever(ragKnowledgeCases),
            new LocalHeuristicLlmAnalyzer());

        var pipelineResult = pipeline.Process(records);
        var (readyForFolderB, needManualReview) = SplitResultsByReviewThreshold(pipelineResult.Items, lowConfidenceThreshold);
        var exporter = new FolderBEnhancedInvoiceExporter();
        var folderBExportResult = exporter.Export(readyForFolderB, folderBPath, outputNamingRule);
        var folderCExportResult = exporter.Export(needManualReview, folderCPath, outputNamingRule);

        var previewLines = pipelineResult.Items
            .Take(3)
            .Select(item =>
                $"- {item.Record.InvoiceNumber}: {item.Decision.DeductibleStatus}/{item.Decision.AccountingCategory} ({item.Decision.ConfidenceScore:0.00})（來源：{ToStageDisplayName(item.ResolvedBy)}）")
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
            $"覆核規則：同一公司來源群組（同檔名基底，-invoice/-allowance 視為同組）只要有任一筆信心度 < {lowConfidenceThreshold:0.00}，該群組檔案即匯出至資料夾 C。";

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
            $"知識案例：內建 {defaultKnowledgeCases.Count} 筆，已核准 {approvedKnowledgeCases.Count} 筆。\n" +
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
            decimal lowConfidenceThreshold)
    {
        var readyForFolderB = new List<InvoiceDecisionResult>(decisionResults.Count);
        var needManualReview = new List<InvoiceDecisionResult>();

        var groupedByReviewGroup = decisionResults
            .GroupBy(item => BuildReviewGroupKey(item.Record.SourceFileName), StringComparer.OrdinalIgnoreCase);

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

    private static string BuildReviewGroupKey(string sourceFileName)
    {
        if (string.IsNullOrWhiteSpace(sourceFileName))
        {
            return string.Empty;
        }

        var normalizedFileName = Path.GetFileName(sourceFileName);
        var baseName = Path.GetFileNameWithoutExtension(normalizedFileName);

        foreach (var suffix in new[] { "-invoice", "-allowance", "-allwance" })
        {
            if (!baseName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var trimmedBaseName = baseName[..^suffix.Length];
            return string.IsNullOrWhiteSpace(trimmedBaseName)
                ? normalizedFileName
                : trimmedBaseName;
        }

        return baseName;
    }

    private static string ResolveConfigPath(string fileName)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "config", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "config", fileName);
    }

    private static string ResolveSolutionRootPath(string configPath)
    {
        var configDirectory = Path.GetDirectoryName(configPath)
            ?? throw new InvalidOperationException("無法解析設定檔目錄路徑。");

        var solutionRootDirectory = Directory.GetParent(configDirectory)?.FullName;
        return solutionRootDirectory ?? AppContext.BaseDirectory;
    }

    private static string ResolveConfiguredPath(string solutionRootPath, string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        return Path.GetFullPath(Path.Combine(solutionRootPath, configuredPath));
    }

    private static IReadOnlyList<CachedInvoiceDecisionCase> CreateDefaultCases()
    {
        return
        [
            new CachedInvoiceDecisionCase(
                VendorTaxId: "65881234",
                ItemName: "雲端伺服器維護費",
                NetAmount: 12000m,
                Decision: new InvoiceDecision(
                    DeductibleStatus: "可抵稅",
                    DeductionReason: "案例快取命中：相同廠商與品名曾判定可抵稅。",
                    AccountingCategory: "資訊服務費",
                    ConfidenceScore: 0.93m)),
            new CachedInvoiceDecisionCase(
                VendorTaxId: "77124567",
                ItemName: "客戶招待餐敘",
                NetAmount: 5600m,
                Decision: new InvoiceDecision(
                    DeductibleStatus: "不可抵稅",
                    DeductionReason: "案例快取命中：交際招待費用不列抵。",
                    AccountingCategory: "交際費",
                    ConfidenceScore: 0.92m))
        ];
    }

    private static IReadOnlyList<KnowledgeReferenceCase> CreateDefaultKnowledgeCases()
    {
        return
        [
            new KnowledgeReferenceCase(
                CaseId: "KB-001",
                Summary: "文具採購通常歸屬辦公用品費並可抵稅。",
                DeductibleStatus: "可抵稅",
                AccountingCategory: "辦公用品費",
                Keywords: ["文具", "耗材", "影印紙"]),
            new KnowledgeReferenceCase(
                CaseId: "KB-002",
                Summary: "招待餐敘通常判定為交際費且不可抵稅。",
                DeductibleStatus: "不可抵稅",
                AccountingCategory: "交際費",
                Keywords: ["招待", "餐敘", "餐飲"]),
            new KnowledgeReferenceCase(
                CaseId: "KB-003",
                Summary: "資訊軟體與維護費通常可列為營業費用。",
                DeductibleStatus: "可抵稅",
                AccountingCategory: "資訊服務費",
                Keywords: ["軟體", "維護", "授權"])
        ];
    }

    private static string ToStageDisplayName(DecisionStage stage)
    {
        return stage switch
        {
            DecisionStage.RuleEngine => "規則引擎",
            DecisionStage.CaseCache => "案例快取",
            DecisionStage.Llm => "本地 LLM",
            _ => "未知來源"
        };
    }

    private sealed record ProcessorStartupState(
        string StatusMessage,
        IReadOnlyList<InvoiceDecisionResult> DecisionResults,
        ManualKnowledgeFeedbackService? FeedbackService);
}

