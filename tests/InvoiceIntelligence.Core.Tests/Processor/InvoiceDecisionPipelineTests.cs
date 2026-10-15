using InvoiceIntelligence.Core.Processor;

namespace InvoiceIntelligence.Core.Tests.Processor;

public class InvoiceDecisionPipelineTests
{
    [Fact]
    public void Constructor_WithNullDependency_ThrowsArgumentNullException()
    {
        var ruleEngine = new StubRuleEngine(null, []);
        var caseCache = new StubCaseCache(null, []);
        var ragRetriever = new StubRagRetriever([], []);
        var llmAnalyzer = new StubLlmAnalyzer(CreateDecision("可抵稅", "ok", "資訊服務費", 0.8m), []);

        Assert.Throws<ArgumentNullException>(() => new InvoiceDecisionPipeline(null!, caseCache, ragRetriever, llmAnalyzer));
        Assert.Throws<ArgumentNullException>(() => new InvoiceDecisionPipeline(ruleEngine, null!, ragRetriever, llmAnalyzer));
        Assert.Throws<ArgumentNullException>(() => new InvoiceDecisionPipeline(ruleEngine, caseCache, null!, llmAnalyzer));
        Assert.Throws<ArgumentNullException>(() => new InvoiceDecisionPipeline(ruleEngine, caseCache, ragRetriever, null!));
    }

    [Fact]
    public void Constructor_WithNonPositiveMaxReferenceCases_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new InvoiceDecisionPipeline(
            new StubRuleEngine(null, []),
            new StubCaseCache(null, []),
            new StubRagRetriever([], []),
            new StubLlmAnalyzer(CreateDecision("可抵稅", "ok", "資訊服務費", 0.8m), []),
            maxReferenceCases: 0));
    }

    [Fact]
    public void Process_WithNullRecords_ThrowsArgumentNullException()
    {
        var pipeline = new InvoiceDecisionPipeline(
            new StubRuleEngine(null, []),
            new StubCaseCache(null, []),
            new StubRagRetriever([], []),
            new StubLlmAnalyzer(CreateDecision("可抵稅", "ok", "資訊服務費", 0.8m), []));

        Assert.Throws<ArgumentNullException>(() => pipeline.Process(null!));
    }

    [Fact]
    public void Process_WhenRagReturnsNull_TreatsReferencesAsEmpty()
    {
        var callLog = new List<string>();
        var llmAnalyzer = new CapturingLlmAnalyzer(CreateDecision("可抵稅", "fallback", "營業費用", 0.75m), callLog);

        var pipeline = new InvoiceDecisionPipeline(
            new StubRuleEngine(null, callLog),
            new StubCaseCache(null, callLog),
            new NullRagRetriever(callLog),
            llmAnalyzer);

        var result = pipeline.Process([CreateRecord("未知支出")]);

        Assert.Single(result.Items);
        Assert.Equal(0, llmAnalyzer.LastReferenceCount);
        Assert.Equal(["rule", "cache", "rag", "llm"], callLog);
    }

    [Fact]
    public void Process_WhenLlmReturnsNull_ThrowsInvalidOperationException()
    {
        var callLog = new List<string>();

        var pipeline = new InvoiceDecisionPipeline(
            new StubRuleEngine(null, callLog),
            new StubCaseCache(null, callLog),
            new StubRagRetriever([], callLog),
            new NullLlmAnalyzer(callLog));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            pipeline.Process([CreateRecord("未知支出")]));

        Assert.Contains("LLM analyzer", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Process_RuleEngineResolvesRecord_SkipsCaseCacheRagAndLlm()
    {
        var callLog = new List<string>();
        var ruleDecision = CreateDecision("可抵稅", "規則命中", "辦公用品費", 0.99m);

        var pipeline = new InvoiceDecisionPipeline(
            new StubRuleEngine(ruleDecision, callLog),
            new StubCaseCache(null, callLog),
            new StubRagRetriever([], callLog),
            new StubLlmAnalyzer(CreateDecision("不可抵稅", "不應被呼叫", "交際費", 0.10m), callLog));

        var result = pipeline.Process([CreateRecord(itemName: "文具")]);

        Assert.Single(result.Items);
        Assert.Equal(DecisionStage.RuleEngine, result.Items[0].ResolvedBy);
        Assert.Equal("可抵稅", result.Items[0].Decision.DeductibleStatus);
        Assert.Equal(1, result.Summary.ResolvedByRuleEngine);
        Assert.Equal(0, result.Summary.ResolvedByCaseCache);
        Assert.Equal(0, result.Summary.ResolvedByLlm);
        Assert.Equal(["rule"], callLog);
    }

    [Fact]
    public void Process_CaseCacheResolvesAfterRuleMiss_SkipsRagAndLlm()
    {
        var callLog = new List<string>();
        var cacheDecision = CreateDecision("不可抵稅", "快取命中", "交際費", 0.92m);

        var pipeline = new InvoiceDecisionPipeline(
            new StubRuleEngine(null, callLog),
            new StubCaseCache(cacheDecision, callLog),
            new StubRagRetriever([], callLog),
            new StubLlmAnalyzer(CreateDecision("可抵稅", "不應被呼叫", "營業費用", 0.10m), callLog));

        var result = pipeline.Process([CreateRecord(itemName: "客戶招待")]);

        Assert.Single(result.Items);
        Assert.Equal(DecisionStage.CaseCache, result.Items[0].ResolvedBy);
        Assert.Equal("不可抵稅", result.Items[0].Decision.DeductibleStatus);
        Assert.Equal(0, result.Summary.ResolvedByRuleEngine);
        Assert.Equal(1, result.Summary.ResolvedByCaseCache);
        Assert.Equal(0, result.Summary.ResolvedByLlm);
        Assert.Equal(["rule", "cache"], callLog);
    }

    [Fact]
    public void Process_UnresolvedRecord_RunsRagThenLlmAndReturnsStructuredDecision()
    {
        var callLog = new List<string>();
        var references = new[]
        {
            new RetrievedReferenceCase("KB-001", "測試案例", "可抵稅", "資訊服務費", 0.87m)
        };

        var llmDecision = CreateDecision("可抵稅", "LLM 判定", "資訊服務費", 0.88m);

        var pipeline = new InvoiceDecisionPipeline(
            new StubRuleEngine(null, callLog),
            new StubCaseCache(null, callLog),
            new StubRagRetriever(references, callLog),
            new StubLlmAnalyzer(llmDecision, callLog));

        var result = pipeline.Process(
        [
            CreateRecord(itemName: "雲端授權費", invoiceNumber: "INV-001")
        ]);

        Assert.Single(result.Items);
        var item = result.Items[0];

        Assert.Equal("INV-001", item.Record.InvoiceNumber);
        Assert.Equal(DecisionStage.Llm, item.ResolvedBy);
        Assert.Equal("可抵稅", item.Decision.DeductibleStatus);
        Assert.Equal("LLM 判定", item.Decision.DeductionReason);
        Assert.Equal("資訊服務費", item.Decision.AccountingCategory);
        Assert.Equal(0.88m, item.Decision.ConfidenceScore);
        Assert.Single(item.ReferenceCases);
        Assert.Equal("KB-001", item.ReferenceCases[0].CaseId);

        Assert.Equal(0, result.Summary.ResolvedByRuleEngine);
        Assert.Equal(0, result.Summary.ResolvedByCaseCache);
        Assert.Equal(1, result.Summary.ResolvedByLlm);
        Assert.Equal(["rule", "cache", "rag", "llm"], callLog);
    }

    private static InvoiceAnalysisRecord CreateRecord(string itemName, string invoiceNumber = "INV-DEFAULT")
    {
        return new InvoiceAnalysisRecord(
            InvoiceNumber: invoiceNumber,
            TaxId: "12345678",
            CompanyName: "Alpha Co",
            VendorTaxId: "87654321",
            ItemName: itemName,
            InvoiceDate: new DateOnly(2026, 1, 15),
            NetAmount: 1200m,
            TaxAmount: 60m,
            TotalAmount: 1260m,
            SourceFileName: "source.xlsx");
    }

    private static InvoiceDecision CreateDecision(
        string status,
        string reason,
        string category,
        decimal confidence)
    {
        return new InvoiceDecision(status, reason, category, confidence);
    }

    private sealed class StubRuleEngine(InvoiceDecision? decision, ICollection<string> callLog) : IRuleEngine
    {
        public InvoiceDecision? TryResolve(InvoiceAnalysisRecord record)
        {
            callLog.Add("rule");
            return decision;
        }
    }

    private sealed class StubCaseCache(InvoiceDecision? decision, ICollection<string> callLog) : ICaseCache
    {
        public InvoiceDecision? TryResolve(InvoiceAnalysisRecord record)
        {
            callLog.Add("cache");
            return decision;
        }
    }

    private sealed class StubRagRetriever(
        IReadOnlyList<RetrievedReferenceCase> referenceCases,
        ICollection<string> callLog)
        : IRagRetriever
    {
        public IReadOnlyList<RetrievedReferenceCase> Retrieve(InvoiceAnalysisRecord record, int maxResults)
        {
            callLog.Add("rag");
            return referenceCases;
        }
    }

    private sealed class StubLlmAnalyzer(InvoiceDecision decision, ICollection<string> callLog) : ILocalLlmAnalyzer
    {
        public InvoiceDecision Analyze(InvoiceAnalysisRecord record, IReadOnlyList<RetrievedReferenceCase> referenceCases)
        {
            callLog.Add("llm");
            return decision;
        }
    }

    private sealed class NullRagRetriever(ICollection<string> callLog) : IRagRetriever
    {
        public IReadOnlyList<RetrievedReferenceCase> Retrieve(InvoiceAnalysisRecord record, int maxResults)
        {
            callLog.Add("rag");
            return null!;
        }
    }

    private sealed class NullLlmAnalyzer(ICollection<string> callLog) : ILocalLlmAnalyzer
    {
        public InvoiceDecision Analyze(InvoiceAnalysisRecord record, IReadOnlyList<RetrievedReferenceCase> referenceCases)
        {
            callLog.Add("llm");
            return null!;
        }
    }

    private sealed class CapturingLlmAnalyzer(InvoiceDecision decision, ICollection<string> callLog) : ILocalLlmAnalyzer
    {
        public int LastReferenceCount { get; private set; }

        public InvoiceDecision Analyze(InvoiceAnalysisRecord record, IReadOnlyList<RetrievedReferenceCase> referenceCases)
        {
            callLog.Add("llm");
            LastReferenceCount = referenceCases.Count;
            return decision;
        }
    }
}
