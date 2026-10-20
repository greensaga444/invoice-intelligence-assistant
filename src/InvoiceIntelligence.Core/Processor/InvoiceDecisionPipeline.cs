namespace InvoiceIntelligence.Core.Processor;

public sealed class InvoiceDecisionPipeline
{
    private readonly IRuleEngine _ruleEngine;
    private readonly ICaseCache _caseCache;
    private readonly IRagRetriever _ragRetriever;
    private readonly ILocalLlmAnalyzer _llmAnalyzer;
    private readonly int _maxReferenceCases;

    public InvoiceDecisionPipeline(
        IRuleEngine ruleEngine,
        ICaseCache caseCache,
        IRagRetriever ragRetriever,
        ILocalLlmAnalyzer llmAnalyzer,
        int maxReferenceCases = 3)
    {
        _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));
        _caseCache = caseCache ?? throw new ArgumentNullException(nameof(caseCache));
        _ragRetriever = ragRetriever ?? throw new ArgumentNullException(nameof(ragRetriever));
        _llmAnalyzer = llmAnalyzer ?? throw new ArgumentNullException(nameof(llmAnalyzer));

        if (maxReferenceCases <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxReferenceCases), "maxReferenceCases must be greater than 0.");
        }

        _maxReferenceCases = maxReferenceCases;
    }

    public InvoiceDecisionPipelineResult Process(
        IReadOnlyCollection<InvoiceAnalysisRecord> records,
        Action<int, int>? reportProgress = null)
    {
        ArgumentNullException.ThrowIfNull(records);

        var results = new List<InvoiceDecisionResult>(records.Count);
        var resolvedByRuleEngine = 0;
        var resolvedByCaseCache = 0;
        var resolvedByLlm = 0;
        var processedCount = 0;
        var totalRecords = records.Count;

        foreach (var record in records)
        {
            var ruleDecision = _ruleEngine.TryResolve(record);
            if (ruleDecision is not null)
            {
                resolvedByRuleEngine++;
                results.Add(new InvoiceDecisionResult(record, ruleDecision, DecisionStage.RuleEngine, []));
                processedCount++;
                reportProgress?.Invoke(processedCount, totalRecords);
                continue;
            }

            var cacheDecision = _caseCache.TryResolve(record);
            if (cacheDecision is not null)
            {
                resolvedByCaseCache++;
                results.Add(new InvoiceDecisionResult(record, cacheDecision, DecisionStage.CaseCache, []));
                processedCount++;
                reportProgress?.Invoke(processedCount, totalRecords);
                continue;
            }

            var references = _ragRetriever.Retrieve(record, _maxReferenceCases) ?? [];
            var llmDecision = _llmAnalyzer.Analyze(record, references)
                ?? throw new InvalidOperationException("LLM analyzer must return a decision.");

            resolvedByLlm++;
            results.Add(new InvoiceDecisionResult(record, llmDecision, DecisionStage.Llm, references));
            processedCount++;
            reportProgress?.Invoke(processedCount, totalRecords);
        }

        var summary = new InvoiceDecisionPipelineSummary(
            TotalRecords: records.Count,
            ResolvedByRuleEngine: resolvedByRuleEngine,
            ResolvedByCaseCache: resolvedByCaseCache,
            ResolvedByLlm: resolvedByLlm);

        return new InvoiceDecisionPipelineResult(results, summary);
    }
}