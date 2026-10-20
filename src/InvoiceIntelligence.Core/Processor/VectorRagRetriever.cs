namespace InvoiceIntelligence.Core.Processor;

public sealed class VectorRagRetriever : IRagRetriever
{
    private readonly IReadOnlyList<CaseEmbedding> _caseEmbeddings;
    private readonly int _vectorDimensions;
    private readonly decimal _minSimilarityScore;

    public VectorRagRetriever(
        IReadOnlyList<KnowledgeReferenceCase> knowledgeCases,
        int vectorDimensions = 256,
        decimal minSimilarityScore = 0.05m)
    {
        _ = knowledgeCases ?? throw new ArgumentNullException(nameof(knowledgeCases));

        if (vectorDimensions <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(vectorDimensions), "vectorDimensions must be greater than 0.");
        }

        if (minSimilarityScore < 0m || minSimilarityScore > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(minSimilarityScore), "minSimilarityScore must be between 0 and 1.");
        }

        _vectorDimensions = vectorDimensions;
        _minSimilarityScore = minSimilarityScore;
        _caseEmbeddings = knowledgeCases
            .Select(item => new CaseEmbedding(item, CreateNormalizedVector(BuildCaseText(item), vectorDimensions)))
            .ToArray();
    }

    public IReadOnlyList<RetrievedReferenceCase> Retrieve(InvoiceAnalysisRecord record, int maxResults)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (maxResults <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResults), "maxResults must be greater than 0.");
        }

        var queryVector = CreateNormalizedVector(record.ItemName, _vectorDimensions);
        if (IsZeroVector(queryVector))
        {
            return [];
        }

        return _caseEmbeddings
            .Select(item => new
            {
                item.Case,
                Similarity = DotProduct(queryVector, item.Embedding)
            })
            .Where(item => item.Similarity >= _minSimilarityScore)
            .OrderByDescending(item => item.Similarity)
            .ThenBy(item => item.Case.CaseId, StringComparer.Ordinal)
            .Take(maxResults)
            .Select(item => new RetrievedReferenceCase(
                CaseId: item.Case.CaseId,
                Summary: item.Case.Summary,
                DeductibleStatus: item.Case.DeductibleStatus,
                AccountingCategory: item.Case.AccountingCategory,
                SimilarityScore: decimal.Round(item.Similarity, 2, MidpointRounding.AwayFromZero)))
            .ToArray();
    }

    private static string BuildCaseText(KnowledgeReferenceCase item)
    {
        return item.Keywords.Count == 0
            ? item.Summary
            : $"{item.Summary} {string.Join(' ', item.Keywords)}";
    }

    private static decimal DotProduct(double[] left, double[] right)
    {
        var sum = 0d;
        for (var i = 0; i < left.Length; i++)
        {
            sum += left[i] * right[i];
        }

        var clamped = Math.Clamp(sum, 0d, 1d);
        return (decimal)clamped;
    }

    private static bool IsZeroVector(double[] vector)
    {
        for (var i = 0; i < vector.Length; i++)
        {
            if (vector[i] != 0d)
            {
                return false;
            }
        }

        return true;
    }

    private static double[] CreateNormalizedVector(string text, int vectorDimensions)
    {
        var vector = new double[vectorDimensions];

        foreach (var feature in EnumerateFeatures(text))
        {
            var bucket = (int)(ComputeFnv1aHash(feature) % (uint)vectorDimensions);
            vector[bucket] += 1d;
        }

        var norm = Math.Sqrt(vector.Sum(value => value * value));
        if (norm == 0d)
        {
            return vector;
        }

        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] /= norm;
        }

        return vector;
    }

    private static IEnumerable<string> EnumerateFeatures(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var normalized = text.Trim().ToLowerInvariant();
        var tokenBuffer = new List<char>(normalized.Length);

        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                tokenBuffer.Add(character);
                continue;
            }

            foreach (var token in EmitToken(tokenBuffer))
            {
                yield return token;
            }
        }

        foreach (var token in EmitToken(tokenBuffer))
        {
            yield return token;
        }
    }

    private static IEnumerable<string> EmitToken(ICollection<char> tokenBuffer)
    {
        if (tokenBuffer.Count == 0)
        {
            yield break;
        }

        var token = new string(tokenBuffer.ToArray());
        tokenBuffer.Clear();

        yield return token;

        if (token.Length >= 2)
        {
            for (var i = 0; i <= token.Length - 2; i++)
            {
                yield return token.Substring(i, 2);
            }
        }

        if (token.Length >= 3)
        {
            for (var i = 0; i <= token.Length - 3; i++)
            {
                yield return token.Substring(i, 3);
            }
        }
    }

    private static uint ComputeFnv1aHash(string value)
    {
        const uint prime = 16777619;
        var hash = 2166136261u;

        foreach (var character in value)
        {
            hash ^= character;
            hash *= prime;
        }

        return hash;
    }

    private sealed record CaseEmbedding(KnowledgeReferenceCase Case, double[] Embedding);
}