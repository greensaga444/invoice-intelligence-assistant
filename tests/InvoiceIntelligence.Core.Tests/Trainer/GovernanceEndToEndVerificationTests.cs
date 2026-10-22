using System.Text.Json;
using ClosedXML.Excel;
using InvoiceIntelligence.Core.Trainer;

namespace InvoiceIntelligence.Core.Tests.Trainer;

public class GovernanceEndToEndVerificationTests : IDisposable
{
    private readonly string _workspaceRootPath;

    public GovernanceEndToEndVerificationTests()
    {
        _workspaceRootPath = Path.Combine(Path.GetTempPath(), "governance-e2e-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspaceRootPath);
        WriteApprovedGovernancePolicy();
    }

    [Fact]
    public void TrainAndEvaluate_WithGovernancePolicyAndDataset_ProducesDeterministicGovernanceArtifacts()
    {
        var datasetPath = CreateDatasetFile([
            new object?[] { "公司A", "2026-01-01", "文具採購", "可抵稅", "辦公用品費", "案例A" },
            new object?[] { " 公司A ", "2026-01-01", " 文具採購 ", "可抵稅", "辦公用品費", "案例A" },
            new object?[] { "公司A", "2026-01-01", "文具採購", "不可抵稅", "交際費", "案例B" },
            new object?[] { "公司B", "2026-02-01", "雲端授權", "可抵稅", "資訊服務費", "案例C" },
            new object?[] { "公司C", "2026-03-01", "餐敘", "不可抵稅", "交際費", "案例D" },
            new object?[] { "公司D", "2026-04-01", "顧問服務", "可抵稅", "顧問費", "案例E" },
            new object?[] { "公司E", "2026-05-01", "運費", "可抵稅", "運費", "案例F" },
            new object?[] { "公司F", "2026-06-01", "系統維護", "可抵稅", "資訊服務費", "案例G" },
            new object?[] { "公司G", "2026-07-01", "", "可抵稅", "資訊服務費", "缺品名" }
        ]);

        var service = new TrainingWorkflowService(_workspaceRootPath);

        var validation = service.ValidateDataset(datasetPath);
        Assert.True(validation.IsValid);
        Assert.Equal(1, validation.GovernanceDuplicateCount);
        Assert.Equal(1, validation.GovernanceConflictCount);
        Assert.Equal(1, validation.InvalidRows);
        Assert.True(validation.SplitLeakageGuardPassed);
        Assert.Equal(
            validation.ValidRows,
            validation.SplitTrainingRows + validation.SplitValidationRows + validation.SplitTestRows);

        Assert.True(File.Exists(validation.SummaryReportPath));
        Assert.True(File.Exists(validation.RejectReportPath));
        Assert.True(File.Exists(validation.ConflictReportPath));

        var summary = JsonSerializer.Deserialize<TrainingGovernanceValidationSummary>(
            File.ReadAllText(validation.SummaryReportPath));
        Assert.NotNull(summary);
        Assert.True(summary.SplitLeakageGuardPassed);
        Assert.Equal(validation.SplitTrainingRows, summary.SplitTrainingRows);
        Assert.Equal(validation.SplitValidationRows, summary.SplitValidationRows);
        Assert.Equal(validation.SplitTestRows, summary.SplitTestRows);

        var train = service.Train(datasetPath, initiatedBy: "tester");
        Assert.True(File.Exists(train.StagedModelPath));
        Assert.Equal(validation.SplitTrainingRows, train.TrainingRecordCount);
        Assert.Equal(validation.SplitValidationRows, train.ValidationRecordCount);
        Assert.Equal(validation.SplitTestRows, train.TestRecordCount);

        var evaluation = service.Evaluate(datasetPath);
        Assert.Equal(validation.SplitTestRows, evaluation.Metrics.TotalTestRecords);
        Assert.True(File.Exists(evaluation.EvaluationReportPath));
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

    private void WriteApprovedGovernancePolicy()
    {
        var governanceDirectory = Path.Combine(_workspaceRootPath, "docs", "config", "governance");
        Directory.CreateDirectory(governanceDirectory);

        var content = """
{
  "policyId": "table3-table4-governance",
  "version": "1.0.0",
  "status": "approved",
  "effectiveFrom": "2020-01-01",
  "approvedBy": "governance-owner",
  "approvedAt": "2020-01-01T00:00:00Z"
}
""";

        File.WriteAllText(
            Path.Combine(governanceDirectory, "table3-table4-governance-policy.v1.json"),
            content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspaceRootPath))
        {
            Directory.Delete(_workspaceRootPath, recursive: true);
        }
    }
}
