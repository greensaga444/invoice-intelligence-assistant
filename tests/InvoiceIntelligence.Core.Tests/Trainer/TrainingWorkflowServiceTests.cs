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

    public void Dispose()
    {
        if (Directory.Exists(_workspaceRootPath))
        {
            Directory.Delete(_workspaceRootPath, recursive: true);
        }
    }
}
