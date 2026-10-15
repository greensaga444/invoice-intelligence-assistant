using System.IO;
using ClosedXML.Excel;
using InvoiceIntelligence.Core.Downloader;

namespace InvoiceIntelligence.Core.Tests.Downloader;

public class CustomerListExcelImporterTests
{
    [Fact]
    public void Import_WhitespacePath_ThrowsArgumentException()
    {
        var importer = new CustomerListExcelImporter();

        Assert.Throws<ArgumentException>(() => importer.Import(" "));
    }

    [Fact]
    public void Import_MissingFile_ThrowsFileNotFoundException()
    {
        var importer = new CustomerListExcelImporter();
        var missingPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.xlsx");

        Assert.Throws<FileNotFoundException>(() => importer.Import(missingPath));
    }

    [Fact]
    public void Import_NonExcelExtension_ThrowsInvalidDataException()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"customer-list-{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempPath, "not excel");
        var importer = new CustomerListExcelImporter();

        try
        {
            Assert.Throws<InvalidDataException>(() => importer.Import(tempPath));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public void Import_ValidWorkbook_ReturnsRecords()
    {
        var filePath = CreateWorkbook([
            ["Tax ID", "Company Name", "Date Range"],
            ["12345678", "Alpha Co", "2026-01-01~2026-01-31"],
            ["87654321", "Beta Co", "2026/02/01~2026/02/28"]
        ]);

        var importer = new CustomerListExcelImporter();

        try
        {
            var result = importer.Import(filePath);

            Assert.Equal(2, result.Records.Count);
            Assert.Equal("12345678", result.Records[0].TaxId);
            Assert.Equal("Alpha Co", result.Records[0].CompanyName);
            Assert.Equal(new DateOnly(2026, 1, 1), result.Records[0].DateFrom);
            Assert.Equal(new DateOnly(2026, 1, 31), result.Records[0].DateTo);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Import_MissingRequiredColumn_ThrowsInvalidDataException()
    {
        var filePath = CreateWorkbook([
            ["Tax ID", "Company Name"],
            ["12345678", "Alpha Co"]
        ]);

        var importer = new CustomerListExcelImporter();

        try
        {
            Assert.Throws<InvalidDataException>(() => importer.Import(filePath));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Import_InvalidDateRange_ThrowsInvalidDataException()
    {
        var filePath = CreateWorkbook([
            ["Tax ID", "Company Name", "Date Range"],
            ["12345678", "Alpha Co", "2026-03-01~2026-02-01"]
        ]);

        var importer = new CustomerListExcelImporter();

        try
        {
            var exception = Assert.Throws<InvalidDataException>(() => importer.Import(filePath));
            Assert.Contains("日期區間", exception.Message);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Import_ChineseAliasesAndFreeFormDateRange_ReturnsParsedRecord()
    {
        var filePath = CreateWorkbook([
            ["統編", "公司名稱", "期間"],
            ["12345678", "Alpha Co", "區間: 2026/03/01 to 2026/03/31"]
        ]);

        var importer = new CustomerListExcelImporter();

        try
        {
            var result = importer.Import(filePath);

            Assert.Single(result.Records);
            Assert.Equal(new DateOnly(2026, 3, 1), result.Records[0].DateFrom);
            Assert.Equal(new DateOnly(2026, 3, 31), result.Records[0].DateTo);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Import_EqualDateRangeBoundary_IsValid()
    {
        var filePath = CreateWorkbook([
            ["Tax ID", "Company Name", "Date Range"],
            ["12345678", "Alpha Co", "2026-04-01~2026-04-01"]
        ]);

        var importer = new CustomerListExcelImporter();

        try
        {
            var result = importer.Import(filePath);

            Assert.Single(result.Records);
            Assert.Equal(new DateOnly(2026, 4, 1), result.Records[0].DateFrom);
            Assert.Equal(new DateOnly(2026, 4, 1), result.Records[0].DateTo);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Import_InvalidTaxId_ThrowsInvalidDataException()
    {
        var filePath = CreateWorkbook([
            ["Tax ID", "Company Name", "Date Range"],
            ["A2345678", "Alpha Co", "2026-03-01~2026-03-31"]
        ]);

        var importer = new CustomerListExcelImporter();

        try
        {
            var exception = Assert.Throws<InvalidDataException>(() => importer.Import(filePath));
            Assert.Contains("統編", exception.Message);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Import_EmptyCompanyName_ThrowsInvalidDataException()
    {
        var filePath = CreateWorkbook([
            ["Tax ID", "Company Name", "Date Range"],
            ["12345678", "", "2026-03-01~2026-03-31"]
        ]);

        var importer = new CustomerListExcelImporter();

        try
        {
            var exception = Assert.Throws<InvalidDataException>(() => importer.Import(filePath));
            Assert.Contains("公司名稱", exception.Message);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Import_OnlyBlankRows_ThrowsInvalidDataException()
    {
        var filePath = CreateWorkbook([
            ["Tax ID", "Company Name", "Date Range"],
            ["", "", ""],
            [" ", " ", " "]
        ]);

        var importer = new CustomerListExcelImporter();

        try
        {
            Assert.Throws<InvalidDataException>(() => importer.Import(filePath));
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static string CreateWorkbook(IEnumerable<string[]> rows)
    {
        var path = Path.Combine(Path.GetTempPath(), $"invoice-intelligence-{Guid.NewGuid():N}.xlsx");

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Customers");

        var rowIndex = 1;
        foreach (var row in rows)
        {
            for (var columnIndex = 0; columnIndex < row.Length; columnIndex++)
            {
                worksheet.Cell(rowIndex, columnIndex + 1).Value = row[columnIndex];
            }

            rowIndex++;
        }

        workbook.SaveAs(path);

        return path;
    }
}
