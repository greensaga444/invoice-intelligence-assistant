using ClosedXML.Excel;
using InvoiceIntelligence.Core.Trainer;

namespace InvoiceIntelligence.Core.Tests.Trainer;

public class Table3Table4CanonicalMapperTests
{
    [Fact]
    public void ResolveWorksheetAndColumns_WithAliasHeaders_ReturnsColumnMapping()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Table3");
        worksheet.Cell(1, 1).Value = "客戶名稱";
        worksheet.Cell(1, 2).Value = "憑證日期";
        worksheet.Cell(1, 3).Value = "摘要";
        worksheet.Cell(1, 4).Value = "是否可抵稅";
        worksheet.Cell(1, 5).Value = "科目";
        worksheet.Cell(1, 6).Value = "原因";

        var mapper = new Table3Table4CanonicalMapper();
        var mapping = mapper.ResolveWorksheetAndColumns(workbook);

        Assert.NotNull(mapping);
        Assert.Equal(worksheet.Name, mapping.Value.Worksheet.Name);
        Assert.Equal(1, mapping.Value.Columns[TrainingCanonicalField.CompanyName]);
        Assert.Equal(2, mapping.Value.Columns[TrainingCanonicalField.InvoiceDate]);
        Assert.Equal(3, mapping.Value.Columns[TrainingCanonicalField.ItemName]);
        Assert.Equal(4, mapping.Value.Columns[TrainingCanonicalField.DeductibleStatus]);
        Assert.Equal(5, mapping.Value.Columns[TrainingCanonicalField.AccountingCategory]);
        Assert.Equal(6, mapping.Value.Columns[TrainingCanonicalField.DeductionReason]);
    }

    [Fact]
    public void ParseRow_WithWhitespaceAndVariantLabels_NormalizesToCanonicalValues()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Table3");
        worksheet.Cell(1, 1).Value = " 公司名稱 ";
        worksheet.Cell(1, 2).Value = "發票日期";
        worksheet.Cell(1, 3).Value = "品名";
        worksheet.Cell(1, 4).Value = "可抵稅";
        worksheet.Cell(1, 5).Value = "建議科目";
        worksheet.Cell(1, 6).Value = "判定原因";

        worksheet.Cell(2, 1).Value = "  公司A  ";
        worksheet.Cell(2, 2).Value = "2026-01-15";
        worksheet.Cell(2, 3).Value = "  雲端   授權  ";
        worksheet.Cell(2, 4).Value = "  可扣抵  ";
        worksheet.Cell(2, 5).Value = "  資訊服務費  ";
        worksheet.Cell(2, 6).Value = "  歷史   案例  ";

        var mapper = new Table3Table4CanonicalMapper();
        var mapping = mapper.ResolveWorksheetAndColumns(workbook);

        Assert.NotNull(mapping);
        var parsed = mapper.ParseRow(mapping.Value.Worksheet, 2, mapping.Value.Columns);

        Assert.False(parsed.IsEmptyRow);
        Assert.Empty(parsed.Issues);
        Assert.NotNull(parsed.Row);
        Assert.Equal("公司A", parsed.Row.CompanyName);
        Assert.Equal(new DateOnly(2026, 1, 15), parsed.Row.InvoiceDate);
        Assert.Equal("雲端 授權", parsed.Row.ItemName);
        Assert.Equal("可抵稅", parsed.Row.DeductibleStatus);
        Assert.Equal("資訊服務費", parsed.Row.AccountingCategory);
        Assert.Equal("歷史 案例", parsed.Row.DeductionReason);
    }

    [Fact]
    public void ParseRow_WithUnknownDeductibleStatus_ReturnsGovernanceVocabularyIssue()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Table3");
        worksheet.Cell(1, 1).Value = "公司名稱";
        worksheet.Cell(1, 2).Value = "發票日期";
        worksheet.Cell(1, 3).Value = "品名";
        worksheet.Cell(1, 4).Value = "可抵稅";
        worksheet.Cell(1, 5).Value = "建議科目";
        worksheet.Cell(1, 6).Value = "判定原因";

        worksheet.Cell(2, 1).Value = "公司A";
        worksheet.Cell(2, 2).Value = "2026-01-15";
        worksheet.Cell(2, 3).Value = "雲端授權";
        worksheet.Cell(2, 4).Value = "未知狀態";
        worksheet.Cell(2, 5).Value = "資訊服務費";
        worksheet.Cell(2, 6).Value = "歷史案例";

        var mapper = new Table3Table4CanonicalMapper();
        var mapping = mapper.ResolveWorksheetAndColumns(workbook);

        Assert.NotNull(mapping);
        var parsed = mapper.ParseRow(mapping.Value.Worksheet, 2, mapping.Value.Columns);

        Assert.False(parsed.IsEmptyRow);
        Assert.Null(parsed.Row);
        Assert.Contains(parsed.Issues, issue => issue.Contains("可抵稅標籤不在治理字典中", StringComparison.Ordinal));
    }
}