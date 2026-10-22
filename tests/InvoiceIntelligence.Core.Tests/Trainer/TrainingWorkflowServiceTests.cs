using ClosedXML.Excel;
using InvoiceIntelligence.Core.Trainer;

namespace InvoiceIntelligence.Core.Tests.Trainer;

public class TrainingWorkflowServiceTests : IDisposable
{
    private readonly string _workspaceRootPath;

    public TrainingWorkflowServiceTests()
    {
        _workspaceRootPath = Path.Combine(Path.GetTempPath(), "training-workflow-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspaceRootPath);
    }

    [Fact]
    public void ValidateDataset_WithRequiredColumns_ReturnsValidationSummary()
    {
        var datasetPath = CreateDatasetFile([
            new object?[] { "公司A", "2026-01-01", "雲端授權", "可抵稅", "資訊服務費", "歷史案例" },
            new object?[] { "公司B", "2026-01-02", "", "可抵稅", "資訊服務費", "缺品名" }
        ]);

        var service = new TrainingWorkflowService(_workspaceRootPath);
        var result = service.ValidateDataset(datasetPath);

        Assert.True(result.IsValid);
        Assert.Equal(2, result.TotalDataRows);
        Assert.Equal(1, result.ValidRows);
        Assert.Equal(1, result.InvalidRows);
        Assert.Contains(result.Issues, issue => issue.Contains("品名為空", StringComparison.Ordinal));
    }

    [Fact]
    public void Train_WithDataset_WritesStagedModel()
    {
        var datasetPath = CreateDatasetFile([
            new object?[] { "公司A", "2026-01-01", "雲端授權", "可抵稅", "資訊服務費", "案例A" },
            new object?[] { "公司B", "2026-02-01", "雲端授權", "可抵稅", "資訊服務費", "案例A" },
            new object?[] { "公司C", "2026-03-01", "餐敘", "不可抵稅", "交際費", "案例B" }
        ]);

        var service = new TrainingWorkflowService(_workspaceRootPath);
        var result = service.Train(datasetPath, initiatedBy: "tester");

        Assert.True(File.Exists(result.StagedModelPath));
        Assert.True(result.ModelEntryCount >= 1);
        Assert.True(result.TrainingRecordCount >= 1);
        Assert.True(result.ValidationRecordCount >= 0);
    }

    [Fact]
    public void Evaluate_WithoutStagedModel_ThrowsInvalidOperationException()
    {
        var datasetPath = CreateDatasetFile([
            new object?[] { "公司A", "2026-01-01", "雲端授權", "可抵稅", "資訊服務費", "案例A" }
        ]);

        var service = new TrainingWorkflowService(_workspaceRootPath);

        Assert.Throws<InvalidOperationException>(() => service.Evaluate(datasetPath));
    }

    [Fact]
    public void Evaluate_WhenStagedModelIsCorrupted_ThrowsJsonException()
    {
        var datasetPath = CreateDatasetFile([
            new object?[] { "公司A", "2026-01-01", "雲端授權", "可抵稅", "資訊服務費", "案例A" }
        ]);

        var service = new TrainingWorkflowService(_workspaceRootPath);
        _ = service.Train(datasetPath, initiatedBy: "tester");
        File.WriteAllText(service.StagedModelPath, "{not-valid-json}");

        Assert.Throws<System.Text.Json.JsonException>(() => service.Evaluate(datasetPath));
    }

    [Fact]
    public void Publish_AfterTrain_CopiesPublishedModel()
    {
        var datasetPath = CreateDatasetFile([
            new object?[] { "公司A", "2026-01-01", "雲端授權", "可抵稅", "資訊服務費", "案例A" },
            new object?[] { "公司B", "2026-02-01", "餐敘", "不可抵稅", "交際費", "案例B" }
        ]);

        var service = new TrainingWorkflowService(_workspaceRootPath);
        _ = service.Train(datasetPath, initiatedBy: "tester");

        var publish = service.Publish("approver");

        Assert.True(File.Exists(publish.PublishedModelPath));
        Assert.Equal("approver", publish.ApprovedBy);
    }

    [Fact]
    public void ValidateDataset_WhenFileDoesNotExist_ThrowsFileNotFoundException()
    {
        var service = new TrainingWorkflowService(_workspaceRootPath);
        var missingPath = Path.Combine(_workspaceRootPath, "missing-dataset.xlsx");

        Assert.Throws<FileNotFoundException>(() => service.ValidateDataset(missingPath));
    }

    [Fact]
    public void ValidateDataset_WithAliasHeaders_ReturnsValidationSummary()
    {
        var datasetPath = CreateDatasetFileWithHeaders(
            ["客戶名稱", "憑證日期", "摘要", "是否可抵稅", "科目", "原因"],
            [
                new object?[] { "公司A", "2026-01-01", "雲端授權", "可抵稅", "資訊服務費", "歷史案例" }
            ]);
        var service = new TrainingWorkflowService(_workspaceRootPath);

        var result = service.ValidateDataset(datasetPath);

        Assert.True(result.IsValid);
        Assert.Equal(1, result.ValidRows);
        Assert.Equal(0, result.InvalidRows);
    }

    [Fact]
    public void Evaluate_WithSameDataset_ProducesDeterministicMetrics()
    {
        var datasetPath = CreateDatasetFile([
            new object?[] { "公司A", "2026-01-01", "雲端授權", "可抵稅", "資訊服務費", "案例A" },
            new object?[] { "公司B", "2026-02-01", "餐敘", "不可抵稅", "交際費", "案例B" },
            new object?[] { "公司C", "2026-03-01", "文具採購", "可抵稅", "辦公用品費", "案例C" },
            new object?[] { "公司D", "2026-04-01", "系統維護", "可抵稅", "資訊服務費", "案例D" },
            new object?[] { "公司E", "2026-05-01", "會議餐點", "不可抵稅", "交際費", "案例E" },
            new object?[] { "公司F", "2026-06-01", "設備租賃", "可抵稅", "租賃費", "案例F" },
            new object?[] { "公司G", "2026-07-01", "運費", "可抵稅", "運費", "案例G" },
            new object?[] { "公司H", "2026-08-01", "教育訓練", "可抵稅", "教育訓練費", "案例H" },
            new object?[] { "公司I", "2026-09-01", "交際禮品", "不可抵稅", "交際費", "案例I" },
            new object?[] { "公司J", "2026-10-01", "雲端儲存", "可抵稅", "資訊服務費", "案例J" }
        ]);

        var service = new TrainingWorkflowService(_workspaceRootPath);
        _ = service.Train(datasetPath, initiatedBy: "tester");

        var first = service.Evaluate(datasetPath);
        var second = service.Evaluate(datasetPath);

        Assert.Equal(first.Metrics.TotalTestRecords, second.Metrics.TotalTestRecords);
        Assert.Equal(first.Metrics.CoveredRecords, second.Metrics.CoveredRecords);
        Assert.Equal(first.Metrics.CoverageRate, second.Metrics.CoverageRate);
        Assert.Equal(first.Metrics.DeductibleAccuracy, second.Metrics.DeductibleAccuracy);
        Assert.Equal(first.Metrics.AccountingCategoryAccuracy, second.Metrics.AccountingCategoryAccuracy);
        Assert.Equal(first.Metrics.DeductionReasonAccuracy, second.Metrics.DeductionReasonAccuracy);
    }

    [Fact]
    public void ValidateDataset_WithDuplicateAndConflictRows_ReturnsGovernanceSummaryCounts()
    {
        var datasetPath = CreateDatasetFile([
            new object?[] { "公司A", "2026-01-01", "文具採購", "可抵稅", "辦公用品費", "案例A" },
            new object?[] { " 公司A ", "2026-01-01", " 文具採購 ", "可抵稅", "辦公用品費", "案例A" },
            new object?[] { "公司A", "2026-01-01", "文具採購", "不可抵稅", "交際費", "案例B" }
        ]);

        var service = new TrainingWorkflowService(_workspaceRootPath);
        var result = service.ValidateDataset(datasetPath);

        Assert.True(result.IsValid);
        Assert.Equal(1, result.GovernanceDuplicateCount);
        Assert.Equal(1, result.GovernanceConflictCount);
        Assert.Equal(1, result.ValidRows);
    }

    [Fact]
    public void ValidateDataset_WithValidRows_WritesQualityGateReportFiles()
    {
        var datasetPath = CreateDatasetFile([
            new object?[] { "公司A", "2026-01-01", "雲端授權", "可抵稅", "資訊服務費", "案例A" },
            new object?[] { "公司B", "2026-01-02", "餐敘", "不可抵稅", "交際費", "案例B" }
        ]);

        var service = new TrainingWorkflowService(_workspaceRootPath);
        var result = service.ValidateDataset(datasetPath);

        Assert.True(File.Exists(result.SummaryReportPath));
        Assert.True(File.Exists(result.RejectReportPath));
        Assert.True(File.Exists(result.ConflictReportPath));
    }

    [Fact]
    public void ValidateDataset_WithValidRows_ReturnsSplitDistributionMetrics()
    {
        var datasetPath = CreateDatasetFile([
            new object?[] { "公司A", "2026-01-01", "雲端授權", "可抵稅", "資訊服務費", "案例A" },
            new object?[] { "公司B", "2026-02-01", "餐敘", "不可抵稅", "交際費", "案例B" },
            new object?[] { "公司C", "2026-03-01", "顧問服務", "可抵稅", "顧問費", "案例C" },
            new object?[] { "公司D", "2026-04-01", "運費", "可抵稅", "運費", "案例D" },
            new object?[] { "公司E", "2026-05-01", "租金", "可抵稅", "租賃費", "案例E" },
            new object?[] { "公司F", "2026-06-01", "文具採購", "可抵稅", "辦公用品費", "案例F" },
            new object?[] { "公司G", "2026-07-01", "教育訓練", "可抵稅", "教育訓練費", "案例G" },
            new object?[] { "公司H", "2026-08-01", "系統維護", "可抵稅", "資訊服務費", "案例H" },
            new object?[] { "公司I", "2026-09-01", "廣告投放", "可抵稅", "廣告費", "案例I" },
            new object?[] { "公司J", "2026-10-01", "會議餐點", "不可抵稅", "交際費", "案例J" }
        ]);

        var service = new TrainingWorkflowService(_workspaceRootPath);
        var result = service.ValidateDataset(datasetPath);

        Assert.True(result.SplitLeakageGuardPassed);
        Assert.Equal(result.ValidRows, result.SplitTrainingRows + result.SplitValidationRows + result.SplitTestRows);
        Assert.Equal(1m, result.SplitTrainingRatio + result.SplitValidationRatio + result.SplitTestRatio);
    }

    [Fact]
    public void ValidateDataset_WithSameCohortRows_KeepsRowsInSinglePartition()
    {
        var datasetPath = CreateDatasetFile([
            new object?[] { "公司A", "2026-01-01", "雲端授權", "可抵稅", "資訊服務費", "案例A" },
            new object?[] { "公司A", "2026-01-15", "系統維護", "可抵稅", "資訊服務費", "案例B" }
        ]);

        var service = new TrainingWorkflowService(_workspaceRootPath);
        var result = service.ValidateDataset(datasetPath);

        Assert.True(result.SplitLeakageGuardPassed);
        var nonZeroPartitions = new[] { result.SplitTrainingRows, result.SplitValidationRows, result.SplitTestRows }
            .Count(value => value > 0);
        Assert.Equal(1, nonZeroPartitions);
    }

    private string CreateDatasetFile(IReadOnlyList<object?[]> rows)
    {
        var datasetPath = Path.Combine(_workspaceRootPath, $"dataset-{Guid.NewGuid():N}.xlsx");

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Table3");
        worksheet.Cell(1, 1).Value = "公司名稱";
        worksheet.Cell(1, 2).Value = "發票日期";
        worksheet.Cell(1, 3).Value = "品名";
        worksheet.Cell(1, 4).Value = "可抵稅";
        worksheet.Cell(1, 5).Value = "建議科目";
        worksheet.Cell(1, 6).Value = "判定原因";

        for (var index = 0; index < rows.Count; index++)
        {
            var rowNumber = index + 2;
            var row = rows[index];
            for (var column = 0; column < row.Length; column++)
            {
                worksheet.Cell(rowNumber, column + 1).SetValue(row[column]?.ToString() ?? string.Empty);
            }
        }

        workbook.SaveAs(datasetPath);
        return datasetPath;
    }

    private string CreateDatasetFileWithHeaders(IReadOnlyList<string> headers, IReadOnlyList<object?[]> rows)
    {
        var datasetPath = Path.Combine(_workspaceRootPath, $"dataset-{Guid.NewGuid():N}.xlsx");

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Table3");

        for (var column = 0; column < headers.Count; column++)
        {
            worksheet.Cell(1, column + 1).Value = headers[column];
        }

        for (var index = 0; index < rows.Count; index++)
        {
            var rowNumber = index + 2;
            var row = rows[index];
            for (var column = 0; column < row.Length; column++)
            {
                worksheet.Cell(rowNumber, column + 1).SetValue(row[column]?.ToString() ?? string.Empty);
            }
        }

        workbook.SaveAs(datasetPath);
        return datasetPath;
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspaceRootPath))
        {
            Directory.Delete(_workspaceRootPath, recursive: true);
        }
    }
}
