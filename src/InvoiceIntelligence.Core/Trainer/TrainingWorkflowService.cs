using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;

namespace InvoiceIntelligence.Core.Trainer;

public sealed class TrainingWorkflowService
{
    private const string StagedModelFileName = "staged-model.json";
    private const string PublishedModelFileName = "published-model.json";
    private const string EvaluationReportFileName = "evaluation-report.json";
    private const string PublishMetadataFileName = "publish-metadata.json";

    private static readonly Table3Table4CanonicalMapper CanonicalMapper = new();
    private static readonly TrainingDatasetGovernancePreprocessor GovernancePreprocessor = new();
    private static readonly TrainingGovernanceQualityGateReporter QualityGateReporter = new();
    private static readonly TrainingGovernancePolicyApprovalLoader GovernancePolicyApprovalLoader = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _artifactDirectoryPath;

    public TrainingWorkflowService(string workspaceRootPath)
    {
        if (string.IsNullOrWhiteSpace(workspaceRootPath))
        {
            throw new ArgumentException("Workspace root path is required.", nameof(workspaceRootPath));
        }

        var governancePolicyPath = Path.Combine(
            workspaceRootPath,
            "docs",
            "config",
            "governance",
            "table3-table4-governance-policy.v1.json");
        _ = GovernancePolicyApprovalLoader.LoadApprovedManifest(governancePolicyPath);

        _artifactDirectoryPath = Path.Combine(workspaceRootPath, "build", "training");
    }

    public string StagedModelPath => Path.Combine(_artifactDirectoryPath, StagedModelFileName);

    public string PublishedModelPath => Path.Combine(_artifactDirectoryPath, PublishedModelFileName);

    public string EvaluationReportPath => Path.Combine(_artifactDirectoryPath, EvaluationReportFileName);

    public TrainingDatasetValidationResult ValidateDataset(string datasetFilePath)
    {
        var dataset = ParseDataset(datasetFilePath);
        var reportPaths = WriteQualityGateReports(dataset);
        return dataset.ValidationResult with
        {
            SummaryReportPath = reportPaths.SummaryReportPath,
            RejectReportPath = reportPaths.RejectReportPath,
            ConflictReportPath = reportPaths.ConflictReportPath
        };
    }

    public TrainingRunResult Train(string datasetFilePath, string initiatedBy)
    {
        if (string.IsNullOrWhiteSpace(initiatedBy))
        {
            throw new ArgumentException("Initiator is required.", nameof(initiatedBy));
        }

        var dataset = ParseDataset(datasetFilePath);
        var reportPaths = WriteQualityGateReports(dataset);
        if (dataset.Records.Count == 0)
        {
            throw new InvalidOperationException("資料集沒有可用資料，無法開始訓練。");
        }

        var split = dataset.Split;
        if (split.TrainingRecords.Count == 0)
        {
            throw new InvalidOperationException("訓練集為空，請確認資料集內容。");
        }

        var model = BuildModel(
            datasetFilePath,
            initiatedBy,
            split.TrainingRecords.Count,
            split.TestRecords.Count,
            split.TrainingRecords);

        Directory.CreateDirectory(_artifactDirectoryPath);
        File.WriteAllText(StagedModelPath, JsonSerializer.Serialize(model, JsonOptions), Encoding.UTF8);

        return new TrainingRunResult(
            dataset.ValidationResult with
            {
                SummaryReportPath = reportPaths.SummaryReportPath,
                RejectReportPath = reportPaths.RejectReportPath,
                ConflictReportPath = reportPaths.ConflictReportPath
            },
            model.Entries.Count,
            split.TrainingRecords.Count,
            split.ValidationRecords.Count,
            split.TestRecords.Count,
            StagedModelPath);
    }

    public TrainingEvaluationResult Evaluate(string datasetFilePath)
    {
        if (!File.Exists(StagedModelPath))
        {
            throw new InvalidOperationException("尚未完成訓練，找不到 staged model。");
        }

        var stagedModel = JsonSerializer.Deserialize<TrainingModelSnapshot>(File.ReadAllText(StagedModelPath, Encoding.UTF8));
        if (stagedModel is null)
        {
            throw new InvalidOperationException("staged model 格式無法解析。");
        }

        var dataset = ParseDataset(datasetFilePath);
        var reportPaths = WriteQualityGateReports(dataset);
        var split = dataset.Split;
        var metrics = EvaluateModel(stagedModel, split.TestRecords);

        Directory.CreateDirectory(_artifactDirectoryPath);
        File.WriteAllText(EvaluationReportPath, JsonSerializer.Serialize(metrics, JsonOptions), Encoding.UTF8);

        return new TrainingEvaluationResult(
            dataset.ValidationResult with
            {
                SummaryReportPath = reportPaths.SummaryReportPath,
                RejectReportPath = reportPaths.RejectReportPath,
                ConflictReportPath = reportPaths.ConflictReportPath
            },
            metrics,
            EvaluationReportPath);
    }

    private TrainingGovernanceReportPaths WriteQualityGateReports(ParsedTrainingDataset dataset)
    {
        return QualityGateReporter.WriteReports(
            _artifactDirectoryPath,
            dataset.ValidationResult,
            dataset.Governance.Conflicts);
    }

    public TrainingPublishResult Publish(string approvedBy)
    {
        if (string.IsNullOrWhiteSpace(approvedBy))
        {
            throw new ArgumentException("Approver is required.", nameof(approvedBy));
        }

        if (!File.Exists(StagedModelPath))
        {
            throw new InvalidOperationException("尚未完成訓練，找不到 staged model。");
        }

        Directory.CreateDirectory(_artifactDirectoryPath);
        File.Copy(StagedModelPath, PublishedModelPath, overwrite: true);

        var metadata = new TrainingPublishMetadata(
            ApprovedBy: approvedBy.Trim(),
            ApprovedAtUtc: DateTimeOffset.UtcNow,
            SourceModelPath: StagedModelPath,
            PublishedModelPath: PublishedModelPath);
        File.WriteAllText(
            Path.Combine(_artifactDirectoryPath, PublishMetadataFileName),
            JsonSerializer.Serialize(metadata, JsonOptions),
            Encoding.UTF8);

        return new TrainingPublishResult(PublishedModelPath, metadata.ApprovedAtUtc, metadata.ApprovedBy);
    }

    private static TrainingModelSnapshot BuildModel(
        string datasetFilePath,
        string initiatedBy,
        int trainingRecordCount,
        int testRecordCount,
        IReadOnlyList<TrainingDatasetRecord> trainingRecords)
    {
        var entries = trainingRecords
            .GroupBy(record => NormalizeKey(record.ItemName), StringComparer.Ordinal)
            .Select(group =>
            {
                var labelGroup = group
                    .GroupBy(item => new LabelTriple(item.DeductibleStatus, item.AccountingCategory, item.DeductionReason))
                    .OrderByDescending(item => item.Count())
                    .ThenBy(item => item.Key.DeductibleStatus, StringComparer.Ordinal)
                    .ThenBy(item => item.Key.AccountingCategory, StringComparer.Ordinal)
                    .ThenBy(item => item.Key.DeductionReason, StringComparer.Ordinal)
                    .First();

                var exemplar = group.First();
                return new TrainingModelEntry(
                    Key: NormalizeKey(exemplar.ItemName),
                    ItemName: exemplar.ItemName,
                    DeductibleStatus: labelGroup.Key.DeductibleStatus,
                    AccountingCategory: labelGroup.Key.AccountingCategory,
                    DeductionReason: labelGroup.Key.DeductionReason,
                    SampleCount: group.Count());
            })
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .ToArray();

        return new TrainingModelSnapshot(
            DatasetFileName: Path.GetFileName(datasetFilePath),
            TrainedBy: initiatedBy.Trim(),
            TrainedAtUtc: DateTimeOffset.UtcNow,
            TrainingRecordCount: trainingRecordCount,
            TestRecordCount: testRecordCount,
            Entries: entries);
    }

    private static TrainingEvaluationMetrics EvaluateModel(
        TrainingModelSnapshot model,
        IReadOnlyList<TrainingDatasetRecord> testRecords)
    {
        if (testRecords.Count == 0)
        {
            return new TrainingEvaluationMetrics(
                EvaluatedAtUtc: DateTimeOffset.UtcNow,
                TotalTestRecords: 0,
                CoveredRecords: 0,
                CoverageRate: 0m,
                DeductibleAccuracy: 0m,
                AccountingCategoryAccuracy: 0m,
                DeductionReasonAccuracy: 0m);
        }

        var dictionary = model.Entries.ToDictionary(item => item.Key, StringComparer.Ordinal);

        var covered = 0;
        var deductibleCorrect = 0;
        var categoryCorrect = 0;
        var reasonCorrect = 0;

        foreach (var record in testRecords)
        {
            if (!dictionary.TryGetValue(NormalizeKey(record.ItemName), out var entry))
            {
                continue;
            }

            covered++;
            if (string.Equals(entry.DeductibleStatus, record.DeductibleStatus, StringComparison.Ordinal))
            {
                deductibleCorrect++;
            }

            if (string.Equals(entry.AccountingCategory, record.AccountingCategory, StringComparison.Ordinal))
            {
                categoryCorrect++;
            }

            if (string.Equals(entry.DeductionReason, record.DeductionReason, StringComparison.Ordinal))
            {
                reasonCorrect++;
            }
        }

        var denominator = covered == 0 ? 1 : covered;
        return new TrainingEvaluationMetrics(
            EvaluatedAtUtc: DateTimeOffset.UtcNow,
            TotalTestRecords: testRecords.Count,
            CoveredRecords: covered,
            CoverageRate: Math.Round((decimal)covered / testRecords.Count, 4),
            DeductibleAccuracy: Math.Round((decimal)deductibleCorrect / denominator, 4),
            AccountingCategoryAccuracy: Math.Round((decimal)categoryCorrect / denominator, 4),
            DeductionReasonAccuracy: Math.Round((decimal)reasonCorrect / denominator, 4));
    }

    private static TrainingDataSplit SplitRecords(IReadOnlyList<TrainingDatasetRecord> records)
    {
        var training = new List<TrainingDatasetRecord>(records.Count);
        var validation = new List<TrainingDatasetRecord>(records.Count / 10 + 1);
        var test = new List<TrainingDatasetRecord>(records.Count / 10 + 1);
        var cohorts = new Dictionary<string, TrainingDatasetPartition>(StringComparer.Ordinal);

        foreach (var record in records)
        {
            var cohortKey = BuildCohortKey(record.CompanyName, record.InvoiceDate);
            var partition = ResolveSplitPartition(record.CompanyName, record.InvoiceDate);

            if (cohorts.TryGetValue(cohortKey, out var previousPartition) && previousPartition != partition)
            {
                throw new InvalidOperationException($"分割政策違反：cohort {cohortKey} 被分配到多個資料分區。");
            }

            cohorts[cohortKey] = partition;

            if (partition == TrainingDatasetPartition.Training)
            {
                training.Add(record);
                continue;
            }

            if (partition == TrainingDatasetPartition.Validation)
            {
                validation.Add(record);
                continue;
            }

            test.Add(record);
        }

        return new TrainingDataSplit(training, validation, test, LeakageGuardPassed: true);
    }

    private static int ComputeSplitBucket(string companyName, DateOnly invoiceDate)
    {
        var key = BuildCohortKey(companyName, invoiceDate);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return bytes[0] % 10;
    }

    private static string BuildCohortKey(string companyName, DateOnly invoiceDate)
    {
        return $"{NormalizeKey(companyName)}|{invoiceDate:yyyy-MM}";
    }

    private static TrainingDatasetPartition ResolveSplitPartition(string companyName, DateOnly invoiceDate)
    {
        var bucket = ComputeSplitBucket(companyName, invoiceDate);
        if (bucket < 8)
        {
            return TrainingDatasetPartition.Training;
        }

        return bucket == 8
            ? TrainingDatasetPartition.Validation
            : TrainingDatasetPartition.Test;
    }

    private static string NormalizeKey(string value)
    {
        return string.Join(
            ' ',
            value
                .Trim()
                .ToLowerInvariant()
                .Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
    }

    private static ParsedTrainingDataset ParseDataset(string datasetFilePath)
    {
        if (string.IsNullOrWhiteSpace(datasetFilePath))
        {
            throw new ArgumentException("Dataset file path is required.", nameof(datasetFilePath));
        }

        if (!File.Exists(datasetFilePath))
        {
            throw new FileNotFoundException("Dataset file not found.", datasetFilePath);
        }

        if (!datasetFilePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("目前僅支援 .xlsx 格式資料集。");
        }

        using var workbook = new XLWorkbook(datasetFilePath);
        var issues = new List<string>();
        var records = new List<TrainingDatasetRecord>();

        var mapping = CanonicalMapper.ResolveWorksheetAndColumns(workbook);
        if (mapping is null)
        {
            issues.Add("找不到符合欄位需求的工作表。需要欄位：公司名稱、發票日期、品名、可抵稅、建議科目、判定原因。");
            return new ParsedTrainingDataset(
                records,
                new TrainingDatasetValidationResult(
                    IsValid: false,
                    WorksheetName: string.Empty,
                    TotalDataRows: 0,
                    ValidRows: 0,
                    InvalidRows: 0,
                    Issues: issues,
                    GovernanceDuplicateCount: 0,
                    GovernanceConflictCount: 0,
                    SplitTrainingRows: 0,
                    SplitValidationRows: 0,
                    SplitTestRows: 0,
                    SplitTrainingRatio: 0m,
                    SplitValidationRatio: 0m,
                    SplitTestRatio: 0m,
                    SplitLeakageGuardPassed: true,
                    SummaryReportPath: string.Empty,
                    RejectReportPath: string.Empty,
                    ConflictReportPath: string.Empty),
                Governance: new TrainingDatasetGovernancePreprocessResult([], 0, 0, []),
                Split: new TrainingDataSplit([], [], [], LeakageGuardPassed: true));
        }

        var (worksheet, columnIndexes) = mapping.Value;
        var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        var totalDataRows = Math.Max(0, lastRowNumber - 1);

        for (var rowNumber = 2; rowNumber <= lastRowNumber; rowNumber++)
        {
            var parsedRow = CanonicalMapper.ParseRow(worksheet, rowNumber, columnIndexes);
            if (parsedRow.IsEmptyRow)
            {
                continue;
            }

            if (parsedRow.Issues.Count > 0)
            {
                issues.Add($"第 {rowNumber} 列：{string.Join("、", parsedRow.Issues)}");
                continue;
            }

            var row = parsedRow.Row!;
            records.Add(new TrainingDatasetRecord(
                CompanyName: row.CompanyName,
                InvoiceDate: row.InvoiceDate,
                ItemName: row.ItemName,
                DeductibleStatus: row.DeductibleStatus,
                AccountingCategory: row.AccountingCategory,
                DeductionReason: row.DeductionReason));
        }

        var governance = GovernancePreprocessor.Apply(records);
        var split = SplitRecords(governance.ProcessedRecords);

        var validation = new TrainingDatasetValidationResult(
            IsValid: governance.ProcessedRecords.Count > 0,
            WorksheetName: worksheet.Name,
            TotalDataRows: totalDataRows,
            ValidRows: governance.ProcessedRecords.Count,
            InvalidRows: issues.Count,
            Issues: issues,
            GovernanceDuplicateCount: governance.DuplicateCount,
            GovernanceConflictCount: governance.ConflictCount,
            SplitTrainingRows: split.TrainingRecords.Count,
            SplitValidationRows: split.ValidationRecords.Count,
            SplitTestRows: split.TestRecords.Count,
            SplitTrainingRatio: split.TrainingRatio,
            SplitValidationRatio: split.ValidationRatio,
            SplitTestRatio: split.TestRatio,
            SplitLeakageGuardPassed: split.LeakageGuardPassed,
            SummaryReportPath: string.Empty,
            RejectReportPath: string.Empty,
            ConflictReportPath: string.Empty);

        return new ParsedTrainingDataset(governance.ProcessedRecords, validation, governance, split);
    }

    private readonly record struct LabelTriple(string DeductibleStatus, string AccountingCategory, string DeductionReason);

    private readonly record struct TrainingDataSplit(
        IReadOnlyList<TrainingDatasetRecord> TrainingRecords,
        IReadOnlyList<TrainingDatasetRecord> ValidationRecords,
        IReadOnlyList<TrainingDatasetRecord> TestRecords,
        bool LeakageGuardPassed)
    {
        private int TotalRows => TrainingRecords.Count + ValidationRecords.Count + TestRecords.Count;

        public decimal TrainingRatio => ComputeRatio(TrainingRecords.Count, TotalRows);

        public decimal ValidationRatio => ComputeRatio(ValidationRecords.Count, TotalRows);

        public decimal TestRatio => ComputeRatio(TestRecords.Count, TotalRows);

        private static decimal ComputeRatio(int value, int total)
        {
            if (total == 0)
            {
                return 0m;
            }

            return Math.Round((decimal)value / total, 4);
        }
    }

    private readonly record struct ParsedTrainingDataset(
        IReadOnlyList<TrainingDatasetRecord> Records,
        TrainingDatasetValidationResult ValidationResult,
        TrainingDatasetGovernancePreprocessResult Governance,
        TrainingDataSplit Split);

    private enum TrainingDatasetPartition
    {
        Training = 0,
        Validation = 1,
        Test = 2
    }
}

public sealed record TrainingDatasetRecord(
    string CompanyName,
    DateOnly InvoiceDate,
    string ItemName,
    string DeductibleStatus,
    string AccountingCategory,
    string DeductionReason);

public sealed record TrainingDatasetValidationResult(
    bool IsValid,
    string WorksheetName,
    int TotalDataRows,
    int ValidRows,
    int InvalidRows,
    IReadOnlyList<string> Issues,
    int GovernanceDuplicateCount,
    int GovernanceConflictCount,
    int SplitTrainingRows,
    int SplitValidationRows,
    int SplitTestRows,
    decimal SplitTrainingRatio,
    decimal SplitValidationRatio,
    decimal SplitTestRatio,
    bool SplitLeakageGuardPassed,
    string SummaryReportPath,
    string RejectReportPath,
    string ConflictReportPath);

public sealed record TrainingRunResult(
    TrainingDatasetValidationResult Validation,
    int ModelEntryCount,
    int TrainingRecordCount,
    int ValidationRecordCount,
    int TestRecordCount,
    string StagedModelPath);

public sealed record TrainingEvaluationResult(
    TrainingDatasetValidationResult Validation,
    TrainingEvaluationMetrics Metrics,
    string EvaluationReportPath);

public sealed record TrainingEvaluationMetrics(
    DateTimeOffset EvaluatedAtUtc,
    int TotalTestRecords,
    int CoveredRecords,
    decimal CoverageRate,
    decimal DeductibleAccuracy,
    decimal AccountingCategoryAccuracy,
    decimal DeductionReasonAccuracy);

public sealed record TrainingPublishResult(
    string PublishedModelPath,
    DateTimeOffset PublishedAtUtc,
    string ApprovedBy);

public sealed record TrainingModelSnapshot(
    string DatasetFileName,
    string TrainedBy,
    DateTimeOffset TrainedAtUtc,
    int TrainingRecordCount,
    int TestRecordCount,
    IReadOnlyList<TrainingModelEntry> Entries);

public sealed record TrainingModelEntry(
    string Key,
    string ItemName,
    string DeductibleStatus,
    string AccountingCategory,
    string DeductionReason,
    int SampleCount);

public sealed record TrainingPublishMetadata(
    string ApprovedBy,
    DateTimeOffset ApprovedAtUtc,
    string SourceModelPath,
    string PublishedModelPath);
