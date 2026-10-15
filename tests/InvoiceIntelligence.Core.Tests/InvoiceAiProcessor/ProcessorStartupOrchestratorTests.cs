using System.Reflection;
using ClosedXML.Excel;
using InvoiceAiProcessor;
using InvoiceIntelligence.Core.Processor;

namespace InvoiceIntelligence.Core.Tests.InvoiceAiProcessor;

public class ProcessorStartupOrchestratorTests
{
    [Fact]
    public void BuildStartupState_WhenFolderAHasNoInputFiles_ReturnsEmptyDecisionResults()
    {
        var orchestrator = new ProcessorStartupOrchestrator();
        var configDirectory = Path.Combine(AppContext.BaseDirectory, "config");
        var configPath = Path.Combine(configDirectory, "processor.ini");

        Directory.CreateDirectory(configDirectory);

        var originalExists = File.Exists(configPath);
        var originalContent = originalExists ? File.ReadAllText(configPath) : null;

        var tempRoot = Path.Combine(AppContext.BaseDirectory, $"processor-startup-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        var iniContent = $@"[module]
    name = Processor-Test

    [paths]
    folderA = {Path.Combine(tempRoot, "folder-a")}
    folderB = {Path.Combine(tempRoot, "folder-b")}
    folderC = {Path.Combine(tempRoot, "folder-c")}

    [output]
    fileNamingRule = {{taxId}}_{{timestamp}}.xlsx

    [review]
    confidenceThreshold = 0.80

    [knowledge]
    repositoryFile = {Path.Combine(tempRoot, "knowledge", "cases.json")}

    [mof]
    account = sample-account
    appKey = sample-key
    ";

        try
        {
            File.WriteAllText(configPath, iniContent);

            var state = orchestrator.BuildStartupState();

            Assert.Empty(state.DecisionResults);
            Assert.NotNull(state.FeedbackService);
            Assert.Contains("載入成功", state.StatusMessage, StringComparison.Ordinal);
            Assert.Contains("資料夾 A 尚無可分析檔案", state.StatusMessage, StringComparison.Ordinal);
        }
        finally
        {
            if (originalExists && originalContent is not null)
            {
                File.WriteAllText(configPath, originalContent);
            }
            else if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void BuildStartupState_WhenFolderAHasWorkbook_ReturnsDecisionSummaryAndExportsFiles()
    {
        var orchestrator = new ProcessorStartupOrchestrator();
        var configDirectory = Path.Combine(AppContext.BaseDirectory, "config");
        var configPath = Path.Combine(configDirectory, "processor.ini");

        Directory.CreateDirectory(configDirectory);

        var originalExists = File.Exists(configPath);
        var originalContent = originalExists ? File.ReadAllText(configPath) : null;

        var tempRoot = Path.Combine(AppContext.BaseDirectory, $"processor-startup-tests-{Guid.NewGuid():N}");
        var folderAPath = Path.Combine(tempRoot, "folder-a");
        var folderBPath = Path.Combine(tempRoot, "folder-b");
        var folderCPath = Path.Combine(tempRoot, "folder-c");

        Directory.CreateDirectory(folderAPath);
        Directory.CreateDirectory(folderBPath);
        Directory.CreateDirectory(folderCPath);

        var sourceFilePath = Path.Combine(folderAPath, "alpha-invoice.xlsx");
        WriteMinimalLegacyWorkbook(sourceFilePath);

        var iniContent = $@"[module]
name = Processor-Test

[paths]
folderA = {folderAPath}
folderB = {folderBPath}
folderC = {folderCPath}

[output]
fileNamingRule = {{taxId}}_{{timestamp}}.xlsx

[review]
confidenceThreshold = 0.80

[knowledge]
repositoryFile = {Path.Combine(tempRoot, "knowledge", "cases.json")}

[mof]
account = sample-account
appKey = sample-key
";

        try
        {
            File.WriteAllText(configPath, iniContent);

            var state = orchestrator.BuildStartupState();

            Assert.NotEmpty(state.DecisionResults);
            Assert.Contains("AI 決策流程完成", state.StatusMessage, StringComparison.Ordinal);
            Assert.Contains("資料夾 B 匯出完成", state.StatusMessage, StringComparison.Ordinal);

            var hasExportedFile = Directory.EnumerateFiles(folderBPath, "*.xlsx", SearchOption.TopDirectoryOnly).Any()
                || Directory.EnumerateFiles(folderCPath, "*.xlsx", SearchOption.TopDirectoryOnly).Any();

            Assert.True(hasExportedFile);
        }
        finally
        {
            if (originalExists && originalContent is not null)
            {
                File.WriteAllText(configPath, originalContent);
            }
            else if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void SplitResultsByReviewThreshold_WhenAnyRecordInGroupBelowThreshold_MovesEntireGroupToManualReview()
    {
        var groupedResults = new List<InvoiceDecisionResult>
        {
            CreateResult("INV-A1", "A-invoice.xlsx", confidenceScore: 0.95m),
            CreateResult("INV-A2", "A-allowance.xlsx", confidenceScore: 0.60m),
            CreateResult("INV-B1", "B-invoice.xlsx", confidenceScore: 0.90m)
        };

        var (readyForFolderB, needManualReview) = InvokeSplitResultsByReviewThreshold(
            groupedResults,
            lowConfidenceThreshold: 0.80m,
            invoiceFileSuffix: "invoice",
            allowanceFileSuffix: "allowance");

        Assert.Single(readyForFolderB);
        Assert.Equal("INV-B1", readyForFolderB[0].Record.InvoiceNumber);

        Assert.Equal(2, needManualReview.Count);
        Assert.Contains(needManualReview, item => item.Record.InvoiceNumber == "INV-A1");
        Assert.Contains(needManualReview, item => item.Record.InvoiceNumber == "INV-A2");
    }

    [Fact]
    public void SplitResultsByReviewThreshold_ConfidenceEqualThreshold_StaysInFolderB()
    {
        var groupedResults = new List<InvoiceDecisionResult>
        {
            CreateResult("INV-C1", "C-invoice.xlsx", confidenceScore: 0.80m),
            CreateResult("INV-C2", "C-allowance.xlsx", confidenceScore: 0.95m)
        };

        var (readyForFolderB, needManualReview) = InvokeSplitResultsByReviewThreshold(
            groupedResults,
            lowConfidenceThreshold: 0.80m,
            invoiceFileSuffix: "invoice",
            allowanceFileSuffix: "allowance");

        Assert.Equal(2, readyForFolderB.Count);
        Assert.Empty(needManualReview);
    }

    private static (IReadOnlyList<InvoiceDecisionResult> ReadyForFolderB, IReadOnlyList<InvoiceDecisionResult> NeedManualReview)
        InvokeSplitResultsByReviewThreshold(
            IReadOnlyCollection<InvoiceDecisionResult> decisionResults,
            decimal lowConfidenceThreshold,
            string invoiceFileSuffix,
            string allowanceFileSuffix)
    {
        var method = typeof(ProcessorStartupOrchestrator).GetMethod(
            "SplitResultsByReviewThreshold",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var result = method!.Invoke(null, [decisionResults, lowConfidenceThreshold, invoiceFileSuffix, allowanceFileSuffix]);
        Assert.NotNull(result);

        var tuple = ((IReadOnlyList<InvoiceDecisionResult> ReadyForFolderB, IReadOnlyList<InvoiceDecisionResult> NeedManualReview))result!;
        return tuple;
    }

    private static InvoiceDecisionResult CreateResult(string invoiceNumber, string sourceFileName, decimal confidenceScore)
    {
        return new InvoiceDecisionResult(
            Record: new InvoiceAnalysisRecord(
                InvoiceNumber: invoiceNumber,
                TaxId: "12345678",
                CompanyName: "Alpha Co",
                VendorTaxId: "87654321",
                ItemName: "Cloud Service",
                InvoiceDate: new DateOnly(2026, 1, 15),
                NetAmount: 100m,
                TaxAmount: 5m,
                TotalAmount: 105m,
                SourceFileName: sourceFileName),
            Decision: new InvoiceDecision(
                DeductibleStatus: "可抵稅",
                DeductionReason: "Rule",
                AccountingCategory: "資訊服務費",
                ConfidenceScore: confidenceScore),
            ResolvedBy: DecisionStage.RuleEngine,
            ReferenceCases: []);
    }

    private static void WriteMinimalLegacyWorkbook(string filePath)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("RawInvoices");

        sheet.Cell(1, 1).Value = "統編";
        sheet.Cell(1, 2).Value = "公司名稱";
        sheet.Cell(1, 3).Value = "發票號碼";
        sheet.Cell(1, 4).Value = "發票日期";
        sheet.Cell(1, 5).Value = "廠商統編";
        sheet.Cell(1, 6).Value = "品名";
        sheet.Cell(1, 7).Value = "未稅金額";
        sheet.Cell(1, 8).Value = "稅額";
        sheet.Cell(1, 9).Value = "含稅金額";

        sheet.Cell(2, 1).Value = "12345678";
        sheet.Cell(2, 2).Value = "Alpha Co";
        sheet.Cell(2, 3).Value = "INV-001";
        sheet.Cell(2, 4).Value = "2026-01-15";
        sheet.Cell(2, 5).Value = "87654321";
        sheet.Cell(2, 6).Value = "辦公文具";
        sheet.Cell(2, 7).Value = 1000m;
        sheet.Cell(2, 8).Value = 50m;
        sheet.Cell(2, 9).Value = 1050m;

        workbook.SaveAs(filePath);
    }
}
