using System.Text.Json;

namespace InvoiceIntelligence.Core.Processor;

public sealed class JsonKnowledgeCaseRepository : IKnowledgeCaseRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly char[] KeywordSeparators =
    [
        ' ',
        '\t',
        '\r',
        '\n',
        ',',
        '.',
        ';',
        ':',
        '-',
        '_',
        '/',
        '\\',
        '(',
        ')',
        '[',
        ']',
        '{',
        '}'
    ];

    public JsonKnowledgeCaseRepository(string repositoryFilePath)
    {
        if (string.IsNullOrWhiteSpace(repositoryFilePath))
        {
            throw new ArgumentException("Repository file path cannot be empty.", nameof(repositoryFilePath));
        }

        RepositoryFilePath = repositoryFilePath;
    }

    public string RepositoryFilePath { get; }

    public IReadOnlyList<KnowledgeReferenceCase> LoadCases()
    {
        var storedCases = LoadStoredCases();
        return storedCases
            .Select(ToReferenceCase)
            .ToArray();
    }

    public KnowledgeFeedbackUpdateResult SaveApprovedResults(
        IReadOnlyCollection<InvoiceDecisionResult> approvedResults,
        string approvedBy,
        DateTimeOffset? approvedAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(approvedResults);

        if (string.IsNullOrWhiteSpace(approvedBy))
        {
            throw new ArgumentException("Approved by value cannot be empty.", nameof(approvedBy));
        }

        if (approvedResults.Count == 0)
        {
            return new KnowledgeFeedbackUpdateResult(0, 0, RepositoryFilePath);
        }

        var now = approvedAtUtc ?? DateTimeOffset.UtcNow;
        var storedCases = LoadStoredCases().ToList();
        var existingFingerprints = storedCases
            .Select(CreateFingerprint)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var addedCount = 0;
        var skippedCount = 0;
        var nextSequence = storedCases.Count + 1;

        foreach (var approvedResult in approvedResults)
        {
            var fingerprint = CreateFingerprint(approvedResult);
            if (!existingFingerprints.Add(fingerprint))
            {
                skippedCount++;
                continue;
            }

            var storedCase = new StoredKnowledgeCase(
                CaseId: BuildCaseId(now, nextSequence),
                Summary: BuildSummary(approvedResult),
                DeductibleStatus: approvedResult.Decision.DeductibleStatus,
                AccountingCategory: approvedResult.Decision.AccountingCategory,
                Keywords: BuildKeywords(approvedResult),
                TaxId: approvedResult.Record.TaxId,
                CompanyName: approvedResult.Record.CompanyName,
                VendorTaxId: approvedResult.Record.VendorTaxId,
                ItemName: approvedResult.Record.ItemName,
                InvoiceNumber: approvedResult.Record.InvoiceNumber,
                ApprovedBy: approvedBy.Trim(),
                ApprovedAtUtc: now);

            storedCases.Add(storedCase);
            addedCount++;
            nextSequence++;
        }

        if (addedCount > 0)
        {
            SaveStoredCases(storedCases);
        }

        return new KnowledgeFeedbackUpdateResult(addedCount, skippedCount, RepositoryFilePath);
    }

    private List<StoredKnowledgeCase> LoadStoredCases()
    {
        if (!File.Exists(RepositoryFilePath))
        {
            return [];
        }

        var content = File.ReadAllText(RepositoryFilePath);
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        try
        {
            var storedCases = JsonSerializer.Deserialize<List<StoredKnowledgeCase>>(content, SerializerOptions);
            return storedCases ?? [];
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Knowledge repository JSON format is invalid.", ex);
        }
    }

    private void SaveStoredCases(IReadOnlyCollection<StoredKnowledgeCase> storedCases)
    {
        var repositoryDirectory = Path.GetDirectoryName(RepositoryFilePath);
        if (!string.IsNullOrWhiteSpace(repositoryDirectory))
        {
            Directory.CreateDirectory(repositoryDirectory);
        }

        var content = JsonSerializer.Serialize(storedCases, SerializerOptions);
        File.WriteAllText(RepositoryFilePath, content);
    }

    private static KnowledgeReferenceCase ToReferenceCase(StoredKnowledgeCase storedCase)
    {
        return new KnowledgeReferenceCase(
            storedCase.CaseId,
            storedCase.Summary,
            storedCase.DeductibleStatus,
            storedCase.AccountingCategory,
            storedCase.Keywords);
    }

    private static string BuildCaseId(DateTimeOffset approvedAtUtc, int sequence)
    {
        return $"KB-FB-{approvedAtUtc:yyyyMMddHHmmss}-{sequence:D4}";
    }

    private static string BuildSummary(InvoiceDecisionResult approvedResult)
    {
        return $"{approvedResult.Record.ItemName} 判定為{approvedResult.Decision.DeductibleStatus}，建議科目為{approvedResult.Decision.AccountingCategory}。";
    }

    private static IReadOnlyList<string> BuildKeywords(InvoiceDecisionResult approvedResult)
    {
        var keywords = new List<string>(capacity: 6);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddKeyword(keywords, seen, approvedResult.Record.ItemName);
        AddKeyword(keywords, seen, approvedResult.Decision.AccountingCategory);
        AddKeyword(keywords, seen, approvedResult.Record.VendorTaxId);

        foreach (var token in approvedResult.Record.ItemName.Split(
                     KeywordSeparators,
                     StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            AddKeyword(keywords, seen, token);
        }

        return keywords.Count == 0 ? ["invoice"] : keywords;
    }

    private static void AddKeyword(List<string> keywords, HashSet<string> seen, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var normalized = value.Trim();
        if (seen.Add(normalized))
        {
            keywords.Add(normalized);
        }
    }

    private static string CreateFingerprint(StoredKnowledgeCase storedCase)
    {
        return string.Join(
            '|',
            Normalize(storedCase.VendorTaxId),
            Normalize(storedCase.ItemName),
            Normalize(storedCase.DeductibleStatus),
            Normalize(storedCase.AccountingCategory));
    }

    private static string CreateFingerprint(InvoiceDecisionResult approvedResult)
    {
        return string.Join(
            '|',
            Normalize(approvedResult.Record.VendorTaxId),
            Normalize(approvedResult.Record.ItemName),
            Normalize(approvedResult.Decision.DeductibleStatus),
            Normalize(approvedResult.Decision.AccountingCategory));
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private sealed record StoredKnowledgeCase(
        string CaseId,
        string Summary,
        string DeductibleStatus,
        string AccountingCategory,
        IReadOnlyList<string> Keywords,
        string TaxId,
        string CompanyName,
        string VendorTaxId,
        string ItemName,
        string InvoiceNumber,
        string ApprovedBy,
        DateTimeOffset ApprovedAtUtc);
}
