using System.Text.Json;

namespace InvoiceIntelligence.Core.Trainer;

internal sealed class TrainingGovernanceQualityGateReporter
{
    private const string QualityGateDirectoryName = "quality-gates";
    private const string SummaryFileName = "validation-summary.json";
    private const string RejectFileName = "reject-report.json";
    private const string ConflictFileName = "conflict-report.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public TrainingGovernanceReportPaths WriteReports(
        string artifactRootDirectory,
        TrainingDatasetValidationResult validation,
        IReadOnlyList<TrainingDatasetConflictAudit> conflicts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactRootDirectory);
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(conflicts);

        var reportDirectory = Path.Combine(artifactRootDirectory, QualityGateDirectoryName);
        Directory.CreateDirectory(reportDirectory);

        var rejectEntries = BuildRejectEntries(validation.Issues);
        var warningEntries = BuildWarningEntries(validation, rejectEntries.Count);

        var summary = new TrainingGovernanceValidationSummary(
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            WorksheetName: validation.WorksheetName,
            TotalDataRows: validation.TotalDataRows,
            ValidRows: validation.ValidRows,
            InvalidRows: validation.InvalidRows,
            DuplicateCount: validation.GovernanceDuplicateCount,
            ConflictCount: validation.GovernanceConflictCount,
            WarningCount: warningEntries.Count);

        var conflictEntries = conflicts
            .Select(conflict => new TrainingGovernanceConflictEntry(
                ConflictKey: conflict.ConflictKey,
                CandidateCount: conflict.CandidateCount,
                SelectedDeductibleStatus: conflict.SelectedDeductibleStatus,
                SelectedAccountingCategory: conflict.SelectedAccountingCategory,
                SelectedDeductionReason: conflict.SelectedDeductionReason,
                CompetingLabels: conflict.CompetingLabels))
            .ToArray();

        var summaryPath = Path.Combine(reportDirectory, SummaryFileName);
        var rejectPath = Path.Combine(reportDirectory, RejectFileName);
        var conflictPath = Path.Combine(reportDirectory, ConflictFileName);

        File.WriteAllText(summaryPath, JsonSerializer.Serialize(summary, JsonOptions));
        File.WriteAllText(
            rejectPath,
            JsonSerializer.Serialize(new TrainingGovernanceRejectReport(rejectEntries, warningEntries), JsonOptions));
        File.WriteAllText(conflictPath, JsonSerializer.Serialize(conflictEntries, JsonOptions));

        return new TrainingGovernanceReportPaths(summaryPath, rejectPath, conflictPath);
    }

    private static IReadOnlyList<TrainingGovernanceRejectEntry> BuildRejectEntries(IReadOnlyList<string> issues)
    {
        var entries = new List<TrainingGovernanceRejectEntry>();

        foreach (var issue in issues)
        {
            if (!TryParseIssueHeader(issue, out var rowNumber, out var reasonText))
            {
                entries.Add(new TrainingGovernanceRejectEntry(
                    RowNumber: -1,
                    Field: "unknown",
                    Reason: issue));
                continue;
            }

            var reasons = reasonText
                .Split('、', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var reason in reasons)
            {
                entries.Add(new TrainingGovernanceRejectEntry(
                    RowNumber: rowNumber,
                    Field: InferField(reason),
                    Reason: reason));
            }
        }

        return entries;
    }

    private static IReadOnlyList<string> BuildWarningEntries(
        TrainingDatasetValidationResult validation,
        int rejectEntryCount)
    {
        var warnings = new List<string>();

        var denominator = validation.TotalDataRows == 0 ? 1m : validation.TotalDataRows;
        var duplicateRatio = (decimal)validation.GovernanceDuplicateCount / denominator;
        if (duplicateRatio > 0.3m)
        {
            warnings.Add($"重複率偏高：{duplicateRatio:P1}（門檻 30%）。");
        }

        if (validation.GovernanceConflictCount > 0)
        {
            warnings.Add($"發現治理衝突：{validation.GovernanceConflictCount} 組。請檢查 conflict report。");
        }

        if (rejectEntryCount > 0 && validation.InvalidRows == 0)
        {
            warnings.Add("偵測到可解析拒絕項目，但 InvalidRows 統計為 0。請檢查資料驗證流程。");
        }

        return warnings;
    }

    private static bool TryParseIssueHeader(string issue, out int rowNumber, out string reasonText)
    {
        rowNumber = -1;
        reasonText = issue;

        if (!issue.StartsWith("第 ", StringComparison.Ordinal))
        {
            return false;
        }

        var markerIndex = issue.IndexOf(" 列：", StringComparison.Ordinal);
        if (markerIndex <= 2)
        {
            return false;
        }

        var rowNumberText = issue[2..markerIndex];
        if (!int.TryParse(rowNumberText, out rowNumber))
        {
            rowNumber = -1;
            return false;
        }

        reasonText = issue[(markerIndex + 3)..];
        return true;
    }

    private static string InferField(string reason)
    {
        var normalized = reason.Trim();
        var marker = normalized.IndexOf('為');
        if (marker > 0)
        {
            return normalized[..marker];
        }

        marker = normalized.IndexOf("格式", StringComparison.Ordinal);
        if (marker > 0)
        {
            return normalized[..marker];
        }

        return "unknown";
    }
}

internal sealed record TrainingGovernanceReportPaths(
    string SummaryReportPath,
    string RejectReportPath,
    string ConflictReportPath);

internal sealed record TrainingGovernanceValidationSummary(
    DateTimeOffset GeneratedAtUtc,
    string WorksheetName,
    int TotalDataRows,
    int ValidRows,
    int InvalidRows,
    int DuplicateCount,
    int ConflictCount,
    int WarningCount);

internal sealed record TrainingGovernanceRejectEntry(
    int RowNumber,
    string Field,
    string Reason);

internal sealed record TrainingGovernanceRejectReport(
    IReadOnlyList<TrainingGovernanceRejectEntry> RejectEntries,
    IReadOnlyList<string> WarningEntries);

internal sealed record TrainingGovernanceConflictEntry(
    string ConflictKey,
    int CandidateCount,
    string SelectedDeductibleStatus,
    string SelectedAccountingCategory,
    string SelectedDeductionReason,
    IReadOnlyList<string> CompetingLabels);