namespace InvoiceIntelligence.Core.Trainer;

internal sealed class TrainingDatasetGovernancePreprocessor
{
    private static readonly char[] WhitespaceSeparators = [' ', '\t', '\r', '\n'];

    public TrainingDatasetGovernancePreprocessResult Apply(IReadOnlyCollection<TrainingDatasetRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        if (records.Count == 0)
        {
            return new TrainingDatasetGovernancePreprocessResult([], 0, 0, []);
        }

        var normalizedRecords = records
            .Select(Normalize)
            .ToArray();

        var duplicateCount = normalizedRecords
            .GroupBy(BuildExactKey, StringComparer.Ordinal)
            .Sum(group => Math.Max(0, group.Count() - 1));

        var deduplicated = normalizedRecords
            .GroupBy(BuildExactKey, StringComparer.Ordinal)
            .Select(group => group
                .OrderBy(row => row.DeductionReason, StringComparer.Ordinal)
                .First())
            .ToArray();

        var conflicts = new List<TrainingDatasetConflictAudit>();
        var selected = new List<TrainingDatasetRecord>();

        var normalizedByIdentity = normalizedRecords
            .GroupBy(BuildConflictIdentity, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        foreach (var identityGroup in deduplicated.GroupBy(BuildConflictIdentity, StringComparer.Ordinal))
        {
            var deduplicatedRows = identityGroup.ToArray();
            var sourceRows = normalizedByIdentity[identityGroup.Key];
            var labels = sourceRows
                .Select(BuildLabelTriple)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(label => label, StringComparer.Ordinal)
                .ToArray();

            if (labels.Length == 1)
            {
                selected.Add(deduplicatedRows[0]);
                continue;
            }

            var winningLabel = sourceRows
                .GroupBy(BuildLabelTriple, StringComparer.Ordinal)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.Ordinal)
                .First()
                .Key;

            var winner = deduplicatedRows
                .Where(row => BuildLabelTriple(row) == winningLabel)
                .OrderBy(row => row.DeductionReason, StringComparer.Ordinal)
                .FirstOrDefault()
                ?? sourceRows
                    .Where(row => BuildLabelTriple(row) == winningLabel)
                    .OrderBy(row => row.DeductionReason, StringComparer.Ordinal)
                    .First();

            selected.Add(winner);
            conflicts.Add(new TrainingDatasetConflictAudit(
                ConflictKey: identityGroup.Key,
                CandidateCount: sourceRows.Length,
                SelectedDeductibleStatus: winner.DeductibleStatus,
                SelectedAccountingCategory: winner.AccountingCategory,
                SelectedDeductionReason: winner.DeductionReason,
                CompetingLabels: labels));
        }

        return new TrainingDatasetGovernancePreprocessResult(
            ProcessedRecords: selected
                .OrderBy(row => row.CompanyName, StringComparer.Ordinal)
                .ThenBy(row => row.InvoiceDate)
                .ThenBy(row => row.ItemName, StringComparer.Ordinal)
                .ToArray(),
            DuplicateCount: duplicateCount,
            ConflictCount: conflicts.Count,
            Conflicts: conflicts
                .OrderBy(conflict => conflict.ConflictKey, StringComparer.Ordinal)
                .ToArray());
    }

    private static TrainingDatasetRecord Normalize(TrainingDatasetRecord row)
    {
        return row with
        {
            CompanyName = NormalizeText(row.CompanyName),
            ItemName = NormalizeText(row.ItemName),
            DeductibleStatus = NormalizeText(row.DeductibleStatus),
            AccountingCategory = NormalizeText(row.AccountingCategory),
            DeductionReason = NormalizeText(row.DeductionReason)
        };
    }

    private static string BuildExactKey(TrainingDatasetRecord row)
    {
        return string.Join(
            '|',
            NormalizeToken(row.CompanyName),
            row.InvoiceDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            NormalizeToken(row.ItemName),
            NormalizeToken(row.DeductibleStatus),
            NormalizeToken(row.AccountingCategory),
            NormalizeToken(row.DeductionReason));
    }

    private static string BuildConflictIdentity(TrainingDatasetRecord row)
    {
        return string.Join(
            '|',
            NormalizeToken(row.CompanyName),
            row.InvoiceDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            NormalizeToken(row.ItemName));
    }

    private static string BuildLabelTriple(TrainingDatasetRecord row)
    {
        return string.Join(
            '|',
            row.DeductibleStatus,
            row.AccountingCategory,
            row.DeductionReason);
    }

    private static string NormalizeToken(string value)
    {
        return NormalizeText(value).ToUpperInvariant();
    }

    private static string NormalizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(' ', value.Trim().Split(WhitespaceSeparators, StringSplitOptions.RemoveEmptyEntries));
    }
}

internal sealed record TrainingDatasetConflictAudit(
    string ConflictKey,
    int CandidateCount,
    string SelectedDeductibleStatus,
    string SelectedAccountingCategory,
    string SelectedDeductionReason,
    IReadOnlyList<string> CompetingLabels);

internal sealed record TrainingDatasetGovernancePreprocessResult(
    IReadOnlyList<TrainingDatasetRecord> ProcessedRecords,
    int DuplicateCount,
    int ConflictCount,
    IReadOnlyList<TrainingDatasetConflictAudit> Conflicts);