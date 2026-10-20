using System.Globalization;
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

    private static readonly IReadOnlyDictionary<TrainingColumnKind, string[]> HeaderAliases =
        new Dictionary<TrainingColumnKind, string[]>
        {
            [TrainingColumnKind.CompanyName] = ["公司名稱", "公司", "客戶名稱", "營業人名稱"],
            [TrainingColumnKind.InvoiceDate] = ["發票日期", "日期", "憑證日期"],
            [TrainingColumnKind.ItemName] = ["品名", "品項", "摘要", "內容"],
            [TrainingColumnKind.DeductibleStatus] = ["可抵稅", "AI_可抵稅", "是否可抵稅"],
            [TrainingColumnKind.AccountingCategory] = ["建議科目", "AI_建議科目", "科目"],
            [TrainingColumnKind.DeductionReason] = ["判定原因", "AI_判定原因", "原因"]
        };

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

        _artifactDirectoryPath = Path.Combine(workspaceRootPath, "build", "training");
    }

    public string StagedModelPath => Path.Combine(_artifactDirectoryPath, StagedModelFileName);

    public string PublishedModelPath => Path.Combine(_artifactDirectoryPath, PublishedModelFileName);

    public string EvaluationReportPath => Path.Combine(_artifactDirectoryPath, EvaluationReportFileName);

    public TrainingDatasetValidationResult ValidateDataset(string datasetFilePath)
    {
        var dataset = ParseDataset(datasetFilePath);
        return dataset.ValidationResult;
    }

    public TrainingRunResult Train(string datasetFilePath, string initiatedBy)
    {
        if (string.IsNullOrWhiteSpace(initiatedBy))
        {
            throw new ArgumentException("Initiator is required.", nameof(initiatedBy));
        }

        var dataset = ParseDataset(datasetFilePath);
        if (dataset.Records.Count == 0)
        {
            throw new InvalidOperationException("資料集沒有可用資料，無法開始訓練。");
        }

        var split = SplitRecords(dataset.Records);
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
            dataset.ValidationResult,
            model.Entries.Count,
            split.TrainingRecords.Count,
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
        var split = SplitRecords(dataset.Records);
        var metrics = EvaluateModel(stagedModel, split.TestRecords);

        Directory.CreateDirectory(_artifactDirectoryPath);
        File.WriteAllText(EvaluationReportPath, JsonSerializer.Serialize(metrics, JsonOptions), Encoding.UTF8);

        return new TrainingEvaluationResult(dataset.ValidationResult, metrics, EvaluationReportPath);
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
        var test = new List<TrainingDatasetRecord>(records.Count / 5 + 1);

        foreach (var record in records)
        {
            var bucket = ComputeSplitBucket(record.CompanyName, record.InvoiceDate);
            if (bucket < 8)
            {
                training.Add(record);
                continue;
            }

            test.Add(record);
        }

        return new TrainingDataSplit(training, test);
    }

    private static int ComputeSplitBucket(string companyName, DateOnly invoiceDate)
    {
        var key = $"{companyName.Trim()}|{invoiceDate:yyyy-MM}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return bytes[0] % 10;
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

        var mapping = ResolveWorksheetAndColumns(workbook);
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
                    Issues: issues));
        }

        var (worksheet, columnIndexes) = mapping.Value;
        var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        var totalDataRows = Math.Max(0, lastRowNumber - 1);

        for (var rowNumber = 2; rowNumber <= lastRowNumber; rowNumber++)
        {
            var companyName = GetCellValue(worksheet, rowNumber, columnIndexes[TrainingColumnKind.CompanyName]);
            var invoiceDateText = GetCellValue(worksheet, rowNumber, columnIndexes[TrainingColumnKind.InvoiceDate]);
            var itemName = GetCellValue(worksheet, rowNumber, columnIndexes[TrainingColumnKind.ItemName]);
            var deductibleStatus = GetCellValue(worksheet, rowNumber, columnIndexes[TrainingColumnKind.DeductibleStatus]);
            var accountingCategory = GetCellValue(worksheet, rowNumber, columnIndexes[TrainingColumnKind.AccountingCategory]);
            var deductionReason = GetCellValue(worksheet, rowNumber, columnIndexes[TrainingColumnKind.DeductionReason]);

            var values = new[] { companyName, invoiceDateText, itemName, deductibleStatus, accountingCategory, deductionReason };
            if (values.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            var rowIssues = new List<string>();
            if (string.IsNullOrWhiteSpace(companyName))
            {
                rowIssues.Add("公司名稱為空");
            }

            if (string.IsNullOrWhiteSpace(itemName))
            {
                rowIssues.Add("品名為空");
            }

            if (string.IsNullOrWhiteSpace(deductibleStatus))
            {
                rowIssues.Add("可抵稅標籤為空");
            }

            if (string.IsNullOrWhiteSpace(accountingCategory))
            {
                rowIssues.Add("建議科目為空");
            }

            if (string.IsNullOrWhiteSpace(deductionReason))
            {
                rowIssues.Add("判定原因為空");
            }

            if (!TryParseDate(invoiceDateText, out var invoiceDate))
            {
                rowIssues.Add("發票日期格式無法解析");
            }

            if (rowIssues.Count > 0)
            {
                issues.Add($"第 {rowNumber} 列：{string.Join("、", rowIssues)}");
                continue;
            }

            records.Add(new TrainingDatasetRecord(
                CompanyName: companyName.Trim(),
                InvoiceDate: invoiceDate,
                ItemName: itemName.Trim(),
                DeductibleStatus: deductibleStatus.Trim(),
                AccountingCategory: accountingCategory.Trim(),
                DeductionReason: deductionReason.Trim()));
        }

        var validation = new TrainingDatasetValidationResult(
            IsValid: records.Count > 0,
            WorksheetName: worksheet.Name,
            TotalDataRows: totalDataRows,
            ValidRows: records.Count,
            InvalidRows: issues.Count,
            Issues: issues);

        return new ParsedTrainingDataset(records, validation);
    }

    private static bool TryParseDate(string input, out DateOnly result)
    {
        var text = input.Trim();
        if (DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
        {
            return true;
        }

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
        {
            result = DateOnly.FromDateTime(dateTime);
            return true;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var serialDate))
        {
            try
            {
                result = DateOnly.FromDateTime(DateTime.FromOADate(serialDate));
                return true;
            }
            catch (ArgumentException)
            {
                // Fall through to return false.
            }
        }

        result = default;
        return false;
    }

    private static (IXLWorksheet Worksheet, Dictionary<TrainingColumnKind, int> Columns)? ResolveWorksheetAndColumns(XLWorkbook workbook)
    {
        foreach (var worksheet in workbook.Worksheets)
        {
            var map = BuildColumnIndexMap(worksheet);
            if (map is not null)
            {
                return (worksheet, map);
            }
        }

        return null;
    }

    private static Dictionary<TrainingColumnKind, int>? BuildColumnIndexMap(IXLWorksheet worksheet)
    {
        var headerRow = worksheet.Row(1);
        var lastColumnNumber = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
        if (lastColumnNumber == 0)
        {
            return null;
        }

        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var column = 1; column <= lastColumnNumber; column++)
        {
            var normalizedHeader = headerRow.Cell(column).GetString().Trim();
            if (string.IsNullOrWhiteSpace(normalizedHeader) || headers.ContainsKey(normalizedHeader))
            {
                continue;
            }

            headers[normalizedHeader] = column;
        }

        var result = new Dictionary<TrainingColumnKind, int>();
        foreach (var pair in HeaderAliases)
        {
            var matchedColumn = pair.Value
                .Select(alias => headers.TryGetValue(alias, out var column) ? column : 0)
                .FirstOrDefault(column => column > 0);
            if (matchedColumn == 0)
            {
                return null;
            }

            result[pair.Key] = matchedColumn;
        }

        return result;
    }

    private static string GetCellValue(IXLWorksheet worksheet, int rowNumber, int columnNumber)
    {
        return worksheet.Cell(rowNumber, columnNumber).GetValue<string>();
    }

    private enum TrainingColumnKind
    {
        CompanyName,
        InvoiceDate,
        ItemName,
        DeductibleStatus,
        AccountingCategory,
        DeductionReason
    }

    private readonly record struct LabelTriple(string DeductibleStatus, string AccountingCategory, string DeductionReason);

    private readonly record struct TrainingDataSplit(
        IReadOnlyList<TrainingDatasetRecord> TrainingRecords,
        IReadOnlyList<TrainingDatasetRecord> TestRecords);

    private readonly record struct ParsedTrainingDataset(
        IReadOnlyList<TrainingDatasetRecord> Records,
        TrainingDatasetValidationResult ValidationResult);
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
    IReadOnlyList<string> Issues);

public sealed record TrainingRunResult(
    TrainingDatasetValidationResult Validation,
    int ModelEntryCount,
    int TrainingRecordCount,
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
