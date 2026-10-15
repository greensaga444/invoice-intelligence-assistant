using ClosedXML.Excel;
using InvoiceIntelligence.Core.Common;

namespace InvoiceIntelligence.Core.Tests.Common;

public class ExcelSchemaHelperTests
{
    [Fact]
    public void ReadHeaderIndex_WithNormalizeEnabled_MapsAliasesToNormalizedKeys()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Data");
        worksheet.Cell(1, 1).Value = "Tax ID";
        worksheet.Cell(1, 2).Value = "Company_Name";

        var headers = ExcelSchemaHelper.ReadHeaderIndex(worksheet, normalize: true);

        Assert.Equal(1, headers["taxid"]);
        Assert.Equal(2, headers["companyname"]);
    }

    [Fact]
    public void GetRequiredColumn_WithAliases_ReturnsMatchedColumn()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Data");
        worksheet.Cell(1, 1).Value = "統編";
        var headers = ExcelSchemaHelper.ReadHeaderIndex(worksheet);

        var column = ExcelSchemaHelper.GetRequiredColumn(headers, ["Tax ID", "統編"], "Tax ID");

        Assert.Equal(1, column);
    }

    [Fact]
    public void GetRequiredColumn_WhenMissing_ThrowsInvalidDataException()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Data");
        worksheet.Cell(1, 1).Value = "A";
        var headers = ExcelSchemaHelper.ReadHeaderIndex(worksheet);

        Assert.Throws<InvalidDataException>(() => ExcelSchemaHelper.GetRequiredColumn(headers, "B"));
    }

    [Fact]
    public void ReadDecimal_WhenCellValueInvalid_ThrowsInvalidDataException()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Data");
        worksheet.Cell(2, 1).Value = "abc";

        Assert.Throws<InvalidDataException>(() => ExcelSchemaHelper.ReadDecimal(worksheet.Cell(2, 1)));
    }

    [Fact]
    public void ReadDecimalOrZero_WhenColumnIsZero_ReturnsZero()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Data");

        var value = ExcelSchemaHelper.ReadDecimalOrZero(worksheet, 2, 0);

        Assert.Equal(0m, value);
    }

    [Fact]
    public void ReadFirstDecimalOrZero_WhenFirstCandidateBlank_UsesNextCandidate()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Data");

        worksheet.Cell(2, 1).Value = string.Empty;
        worksheet.Cell(2, 2).Value = 123.45m;

        var value = ExcelSchemaHelper.ReadFirstDecimalOrZero(worksheet, 2, 1, 2);

        Assert.Equal(123.45m, value);
    }

    [Fact]
    public void TryGetColumn_WhenNameMissing_ReturnsZero()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Data");
        worksheet.Cell(1, 1).Value = "Tax ID";
        var headers = ExcelSchemaHelper.ReadHeaderIndex(worksheet);

        var column = ExcelSchemaHelper.TryGetColumn(headers, "NotExists");

        Assert.Equal(0, column);
    }

    [Fact]
    public void NormalizeHeader_RemovesSpacesAndUnderscoresAndLowercases()
    {
        var normalized = ExcelSchemaHelper.NormalizeHeader(" Company_Name ");

        Assert.Equal("companyname", normalized);
    }
}
