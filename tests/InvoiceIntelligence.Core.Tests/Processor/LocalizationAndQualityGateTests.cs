using System.Diagnostics;
using System.IO;
using ClosedXML.Excel;
using InvoiceIntelligence.Core.Processor;

namespace InvoiceIntelligence.Core.Tests.Processor;

public class LocalizationAndQualityGateTests
{
    [Fact]
    public void LocalHeuristicLlmAnalyzer_ReturnsTraditionalChineseBusinessFields()
    {
        var analyzer = new LocalHeuristicLlmAnalyzer();
        var record = CreateRecord(itemName: "客戶招待餐敘", vendorTaxId: "87654321", netAmount: 8000m);

        var decision = analyzer.Analyze(record, []);

        Assert.Equal("不可抵稅", decision.DeductibleStatus);
        Assert.Equal("交際費", decision.AccountingCategory);
        Assert.Contains("本地", decision.DeductionReason);
    }

    [Fact]
    public void FolderBEnhancedInvoiceExporter_UsesTraditionalChineseWorksheetName()
    {
        var outputDirectory = CreateTempDirectory();
        var exporter = new FolderBEnhancedInvoiceExporter();

        var result = new InvoiceDecisionResult(
            Record: CreateRecord(itemName: "辦公文具", vendorTaxId: "87654321", netAmount: 1000m),
            Decision: new InvoiceDecision("可抵稅", "符合營業用途。", "辦公用品費", 0.9m),
            ResolvedBy: DecisionStage.RuleEngine,
            ReferenceCases: []);

        try
        {
            var exportResult = exporter.Export(
                [result],
                outputDirectory,
                "{taxId}_{companyName}_{dateFrom}_{dateTo}.xlsx",
                new DateTimeOffset(2026, 10, 12, 0, 0, 0, TimeSpan.Zero));

            using var workbook = new XLWorkbook(exportResult.Files[0].OutputFilePath);
            Assert.Equal("銷項發票明細檔(表三)", workbook.Worksheet(1).Name);
        }
        finally
        {
            DeleteDirectory(outputDirectory);
        }
    }

    [Fact]
    public void ProcessorCodePath_DoesNotContainExternalAiInvocationPatterns()
    {
        var repositoryRoot = ResolveRepositoryRoot();
        var processorDirectory = Path.Combine(repositoryRoot, "src", "InvoiceIntelligence.Core", "Processor");
        var sourceFiles = Directory.GetFiles(processorDirectory, "*.cs", SearchOption.TopDirectoryOnly);

        var disallowedPatterns = new[]
        {
            "HttpClient",
            "http://",
            "https://",
            "api.openai",
            "azureopenai",
            "anthropic",
            "gemini"
        };

        var violations = new List<string>();

        foreach (var filePath in sourceFiles)
        {
            var content = File.ReadAllText(filePath);
            foreach (var pattern in disallowedPatterns)
            {
                if (content.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    violations.Add($"{Path.GetFileName(filePath)} contains '{pattern}'");
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void InvoiceDecisionPipeline_ProcessThirtyThousandRecords_CompletesWithinScaleTarget()
    {
        var pipeline = new InvoiceDecisionPipeline(
            new KeywordRuleEngine(),
            new InMemoryCaseCache(
            [
                new CachedInvoiceDecisionCase(
                    VendorTaxId: "90000000",
                    ItemName: "雲端授權費",
                    NetAmount: 2200m,
                    Decision: new InvoiceDecision("可抵稅", "案例快取命中。", "資訊服務費", 0.95m))
            ]),
            new VectorRagRetriever(
            [
                new KnowledgeReferenceCase("KB-001", "文具案例", "可抵稅", "辦公用品費", ["文具", "耗材"]),
                new KnowledgeReferenceCase("KB-002", "顧問服務案例", "可抵稅", "營業費用", ["顧問", "服務"])
            ]),
            new LocalHeuristicLlmAnalyzer());

        var records = Enumerable.Range(1, 30000)
            .Select(CreateScaleRecord)
            .ToArray();

        var stopwatch = Stopwatch.StartNew();
        var result = pipeline.Process(records);
        stopwatch.Stop();

        Assert.Equal(30000, result.Summary.TotalRecords);
        Assert.Equal(10000, result.Summary.ResolvedByRuleEngine);
        Assert.Equal(10000, result.Summary.ResolvedByCaseCache);
        Assert.Equal(10000, result.Summary.ResolvedByLlm);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(5),
            $"Pipeline processing took {stopwatch.Elapsed.TotalSeconds:0.00}s, expected under 5 seconds.");
    }

    private static InvoiceAnalysisRecord CreateScaleRecord(int index)
    {
        var route = index % 3;

        if (route == 1)
        {
            return CreateRecord(
                itemName: "辦公文具",
                vendorTaxId: $"{80000000 + (index % 97):D8}",
                netAmount: 1200m,
                invoiceNumber: $"INV-RULE-{index:00000}");
        }

        if (route == 2)
        {
            return CreateRecord(
                itemName: "雲端授權費",
                vendorTaxId: "90000000",
                netAmount: 2200m,
                invoiceNumber: $"INV-CACHE-{index:00000}");
        }

        return CreateRecord(
            itemName: "一般顧問服務費",
            vendorTaxId: $"{70000000 + (index % 79):D8}",
            netAmount: 3600m,
            invoiceNumber: $"INV-LLM-{index:00000}");
    }

    private static InvoiceAnalysisRecord CreateRecord(
        string itemName,
        string vendorTaxId,
        decimal netAmount,
        string invoiceNumber = "INV-001")
    {
        var taxAmount = decimal.Round(netAmount * 0.05m, 2, MidpointRounding.AwayFromZero);

        return new InvoiceAnalysisRecord(
            InvoiceNumber: invoiceNumber,
            TaxId: "12345678",
            CompanyName: "Alpha Co",
            VendorTaxId: vendorTaxId,
            ItemName: itemName,
            InvoiceDate: new DateOnly(2026, 1, 15),
            NetAmount: netAmount,
            TaxAmount: taxAmount,
            TotalAmount: netAmount + taxAmount,
            SourceFileName: "source.xlsx");
    }

    private static string ResolveRepositoryRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"invoice-quality-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, true);
        }
    }
}
