using System.Text.Json;
using InvoiceIntelligence.Core.Trainer;

namespace InvoiceIntelligence.Core.Tests.Trainer;

public class TrainingGovernanceQualityGateReporterTests : IDisposable
{
    private readonly string _tempDirectory;

    public TrainingGovernanceQualityGateReporterTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "training-governance-reports", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void WriteReports_WithValidationAndConflictData_WritesSummaryRejectAndConflictFiles()
    {
        var reporter = new TrainingGovernanceQualityGateReporter();
        var validation = new TrainingDatasetValidationResult(
            IsValid: true,
            WorksheetName: "Table3",
            TotalDataRows: 10,
            ValidRows: 8,
            InvalidRows: 2,
            Issues: ["第 3 列：公司名稱為空", "第 5 列：發票日期格式無法解析"],
            GovernanceDuplicateCount: 4,
            GovernanceConflictCount: 1,
            SplitTrainingRows: 6,
            SplitValidationRows: 1,
            SplitTestRows: 1,
            SplitTrainingRatio: 0.75m,
            SplitValidationRatio: 0.125m,
            SplitTestRatio: 0.125m,
            SplitLeakageGuardPassed: true,
            SummaryReportPath: string.Empty,
            RejectReportPath: string.Empty,
            ConflictReportPath: string.Empty);
        var conflicts = new[]
        {
            new TrainingDatasetConflictAudit(
                ConflictKey: "COMPANYA|2026-01-01|文具採購",
                CandidateCount: 2,
                SelectedDeductibleStatus: "可抵稅",
                SelectedAccountingCategory: "辦公用品費",
                SelectedDeductionReason: "案例A",
                CompetingLabels: ["可抵稅|辦公用品費|案例A", "不可抵稅|交際費|案例B"])
        };

        var paths = reporter.WriteReports(_tempDirectory, validation, conflicts);

        Assert.True(File.Exists(paths.SummaryReportPath));
        Assert.True(File.Exists(paths.RejectReportPath));
        Assert.True(File.Exists(paths.ConflictReportPath));

        var summaryJson = File.ReadAllText(paths.SummaryReportPath);
        var summary = JsonSerializer.Deserialize<TrainingGovernanceValidationSummary>(summaryJson);
        Assert.NotNull(summary);
        Assert.Equal(4, summary.DuplicateCount);
        Assert.Equal(1, summary.ConflictCount);
        Assert.Equal(6, summary.SplitTrainingRows);
        Assert.Equal(1, summary.SplitValidationRows);
        Assert.Equal(1, summary.SplitTestRows);
        Assert.True(summary.SplitLeakageGuardPassed);

        var rejectJson = File.ReadAllText(paths.RejectReportPath);
        var rejectReport = JsonSerializer.Deserialize<TrainingGovernanceRejectReport>(rejectJson);
        Assert.NotNull(rejectReport);
        Assert.True(rejectReport.RejectEntries.Count >= 2);
        Assert.True(rejectReport.WarningEntries.Count >= 1);

        var conflictJson = File.ReadAllText(paths.ConflictReportPath);
        var conflictEntries = JsonSerializer.Deserialize<List<TrainingGovernanceConflictEntry>>(conflictJson);
        Assert.NotNull(conflictEntries);
        Assert.Single(conflictEntries);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}