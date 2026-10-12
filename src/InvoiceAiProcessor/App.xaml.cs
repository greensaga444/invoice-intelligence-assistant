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

        string message;

        try
        {
            var loader = new IniModuleConfigurationLoader();
            var configPath = ResolveConfigPath("processor.ini");
            var solutionRootPath = ResolveSolutionRootPath(configPath);
            var config = loader.Load(configPath);
            var folderAPath = ResolveConfiguredPath(solutionRootPath, config.FolderAPath);
            var folderBPath = ResolveConfiguredPath(solutionRootPath, config.FolderBPath);

            var pipelineStatus = BuildPipelineStatus(folderAPath, folderBPath, config.OutputNamingRule);

            message = $"載入成功: {config.ModuleName}\n" +
                      $"Folder A: {folderAPath}\n" +
                      $"Folder B: {folderBPath}\n" +
                      $"命名規則: {config.OutputNamingRule}\n" +
                      pipelineStatus;
        }
        catch (Exception ex)
        {
            message = $"設定載入失敗: {ex.Message}";
        }

        var mainWindow = new MainWindow(message);
        mainWindow.Show();
    }

    private static string BuildPipelineStatus(string folderAPath, string folderBPath, string outputNamingRule)
    {
        var reader = new FolderAInvoiceRecordReader();
        var records = reader.ReadFromFolder(folderAPath);

        if (records.Count == 0)
        {
            return "AI 決策流程：Folder A 尚無可分析檔案。";
        }

        var pipeline = new InvoiceDecisionPipeline(
            new KeywordRuleEngine(),
            new InMemoryCaseCache(CreateDefaultCases()),
            new KeywordRagRetriever(CreateDefaultKnowledgeCases()),
            new LocalHeuristicLlmAnalyzer());

        var pipelineResult = pipeline.Process(records);
        var exporter = new FolderBEnhancedInvoiceExporter();
        var exportResult = exporter.Export(pipelineResult.Items, folderBPath, outputNamingRule);

        var previewLines = pipelineResult.Items
            .Take(3)
            .Select(item =>
                $"- {item.Record.InvoiceNumber}: {item.Decision.DeductibleStatus}/{item.Decision.AccountingCategory} ({item.Decision.ConfidenceScore:0.00}) via {ToStageDisplayName(item.ResolvedBy)}")
            .ToArray();

        var exportedFilesPreview = exportResult.Files
            .Take(3)
            .Select(file =>
                $"- {Path.GetFileName(file.OutputFilePath)} ({file.RecordCount} 筆)")
            .ToArray();

        var exportSummary =
            $"Folder B 匯出完成：檔案 {exportResult.ExportedFileCount}，筆數 {exportResult.ExportedRecordCount}。";

        if (exportedFilesPreview.Length > 0)
        {
            exportSummary += $"\n輸出預覽:\n{string.Join("\n", exportedFilesPreview)}";
        }

        return
            $"AI 決策流程完成：總筆數 {pipelineResult.Summary.TotalRecords}，規則 {pipelineResult.Summary.ResolvedByRuleEngine}，快取 {pipelineResult.Summary.ResolvedByCaseCache}，LLM {pipelineResult.Summary.ResolvedByLlm}。\n" +
            $"判定預覽:\n{string.Join("\n", previewLines)}\n" +
            exportSummary;
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
            ?? throw new InvalidOperationException("Cannot resolve config directory path.");

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
            DecisionStage.RuleEngine => "Rule Engine",
            DecisionStage.CaseCache => "Case Cache",
            DecisionStage.Llm => "LLM",
            _ => "Unknown"
        };
    }
}

