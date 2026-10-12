namespace InvoiceIntelligence.Core.Processor;

public sealed class KeywordRagRetriever : IRagRetriever
{
    private readonly IReadOnlyList<KnowledgeReferenceCase> _knowledgeCases;

    public KeywordRagRetriever(IReadOnlyList<KnowledgeReferenceCase> knowledgeCases)
    {
        _knowledgeCases = knowledgeCases ?? throw new ArgumentNullException(nameof(knowledgeCases));
    }

    public IReadOnlyList<RetrievedReferenceCase> Retrieve(InvoiceAnalysisRecord record, int maxResults)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (maxResults <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResults), "maxResults must be greater than 0.");
        }

        var rankedCases = _knowledgeCases
            .Select(item => new
            {
                Case = item,
                Similarity = CalculateSimilarity(record.ItemName, item.Keywords)
            })
            .Where(item => item.Similarity > 0m)
            .OrderByDescending(item => item.Similarity)
            .ThenBy(item => item.Case.CaseId, StringComparer.Ordinal)
            .Take(maxResults)
            .Select(item => new RetrievedReferenceCase(
                CaseId: item.Case.CaseId,
                Summary: item.Case.Summary,
                DeductibleStatus: item.Case.DeductibleStatus,
                AccountingCategory: item.Case.AccountingCategory,
                SimilarityScore: item.Similarity))
            .ToArray();

        return rankedCases;
    }

    private static decimal CalculateSimilarity(string itemName, IReadOnlyList<string> keywords)
    {
        if (string.IsNullOrWhiteSpace(itemName) || keywords.Count == 0)
        {
            return 0m;
        }

        var matchedCount = keywords.Count(keyword =>
            !string.IsNullOrWhiteSpace(keyword)
            && itemName.Contains(keyword.Trim(), StringComparison.OrdinalIgnoreCase));

        if (matchedCount == 0)
        {
            return 0m;
        }

        return decimal.Round((decimal)matchedCount / keywords.Count, 2, MidpointRounding.AwayFromZero);
    }
}
