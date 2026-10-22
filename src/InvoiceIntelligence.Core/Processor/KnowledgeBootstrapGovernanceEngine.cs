namespace InvoiceIntelligence.Core.Processor;

public sealed class KnowledgeBootstrapGovernanceEngine
{
    private static readonly char[] WhitespaceSeparators = [' ', '\t', '\r', '\n'];

    public KnowledgeBootstrapGovernanceResult Resolve(IReadOnlyCollection<KnowledgeBootstrapCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        if (candidates.Count == 0)
        {
            return new KnowledgeBootstrapGovernanceResult([], 0, 0, []);
        }

        var normalized = candidates
            .Select(Normalize)
            .ToArray();

        var duplicateCount = normalized
            .GroupBy(BuildFingerprint, StringComparer.Ordinal)
            .Sum(group => Math.Max(0, group.Count() - 1));

        var deduplicated = normalized
            .GroupBy(BuildFingerprint, StringComparer.Ordinal)
            .Select(group => group
                .OrderByDescending(candidate => candidate.Source)
                .ThenByDescending(candidate => candidate.InvoiceDate ?? DateOnly.MinValue)
                .ThenBy(candidate => candidate.DeductionReason, StringComparer.Ordinal)
                .First())
            .ToArray();

        var conflicts = new List<KnowledgeBootstrapConflictAudit>();
        var accepted = new List<KnowledgeBootstrapCandidate>();

        var originalByIdentity = normalized
            .GroupBy(BuildConflictIdentity, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        foreach (var identityGroup in deduplicated.GroupBy(BuildConflictIdentity, StringComparer.Ordinal))
        {
            var selected = SelectWinner(identityGroup.ToArray());
            accepted.Add(selected);

            var sourceGroup = originalByIdentity[identityGroup.Key];
            var competingLabels = sourceGroup
                .Select(candidate => BuildLabelTriple(candidate))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(label => label, StringComparer.Ordinal)
                .ToArray();

            if (competingLabels.Length <= 1)
            {
                continue;
            }

            conflicts.Add(new KnowledgeBootstrapConflictAudit(
                ConflictKey: identityGroup.Key,
                CandidateCount: sourceGroup.Length,
                SelectedDeductibleStatus: selected.DeductibleStatus,
                SelectedAccountingCategory: selected.AccountingCategory,
                SelectedDeductionReason: selected.DeductionReason,
                CompetingLabels: competingLabels));
        }

        return new KnowledgeBootstrapGovernanceResult(
            AcceptedRows: accepted
                .OrderBy(candidate => candidate.VendorTaxId, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.ItemName, StringComparer.Ordinal)
                .ToArray(),
            DuplicateCount: duplicateCount,
            ConflictCount: conflicts.Count,
            Conflicts: conflicts
                .OrderBy(conflict => conflict.ConflictKey, StringComparer.Ordinal)
                .ToArray());
    }

    private static KnowledgeBootstrapCandidate SelectWinner(IReadOnlyList<KnowledgeBootstrapCandidate> candidates)
    {
        var topSource = candidates.Max(candidate => candidate.Source);
        var sourceFiltered = candidates
            .Where(candidate => candidate.Source == topSource)
            .ToArray();

        var newestDate = sourceFiltered.Max(candidate => candidate.InvoiceDate ?? DateOnly.MinValue);
        var dateFiltered = sourceFiltered
            .Where(candidate => (candidate.InvoiceDate ?? DateOnly.MinValue) == newestDate)
            .ToArray();

        var majorityTriple = dateFiltered
            .GroupBy(BuildLabelTriple, StringComparer.Ordinal)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .First();

        return dateFiltered
            .Where(candidate => BuildLabelTriple(candidate) == majorityTriple.Key)
            .OrderBy(candidate => candidate.DeductionReason, StringComparer.Ordinal)
            .First();
    }

    private static string BuildFingerprint(KnowledgeBootstrapCandidate candidate)
    {
        return string.Join(
            '|',
            NormalizeToken(candidate.VendorTaxId),
            NormalizeToken(candidate.ItemName),
            NormalizeToken(candidate.DeductibleStatus),
            NormalizeToken(candidate.AccountingCategory));
    }

    private static string BuildConflictIdentity(KnowledgeBootstrapCandidate candidate)
    {
        return string.Join(
            '|',
            NormalizeToken(candidate.VendorTaxId),
            NormalizeToken(candidate.ItemName));
    }

    private static string BuildLabelTriple(KnowledgeBootstrapCandidate candidate)
    {
        return string.Join(
            '|',
            candidate.DeductibleStatus,
            candidate.AccountingCategory,
            candidate.DeductionReason);
    }

    private static KnowledgeBootstrapCandidate Normalize(KnowledgeBootstrapCandidate candidate)
    {
        return candidate with
        {
            VendorTaxId = NormalizeToken(candidate.VendorTaxId),
            ItemName = NormalizeText(candidate.ItemName),
            DeductibleStatus = NormalizeText(candidate.DeductibleStatus),
            AccountingCategory = NormalizeText(candidate.AccountingCategory),
            DeductionReason = NormalizeText(candidate.DeductionReason)
        };
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

public enum KnowledgeLabelSource
{
    HeuristicGenerated = 1,
    GovernedBootstrapImport = 2,
    ManualApprovedFeedback = 3
}

public sealed record KnowledgeBootstrapCandidate(
    string VendorTaxId,
    string ItemName,
    string DeductibleStatus,
    string AccountingCategory,
    string DeductionReason,
    DateOnly? InvoiceDate,
    KnowledgeLabelSource Source);

public sealed record KnowledgeBootstrapConflictAudit(
    string ConflictKey,
    int CandidateCount,
    string SelectedDeductibleStatus,
    string SelectedAccountingCategory,
    string SelectedDeductionReason,
    IReadOnlyList<string> CompetingLabels);

public sealed record KnowledgeBootstrapGovernanceResult(
    IReadOnlyList<KnowledgeBootstrapCandidate> AcceptedRows,
    int DuplicateCount,
    int ConflictCount,
    IReadOnlyList<KnowledgeBootstrapConflictAudit> Conflicts);