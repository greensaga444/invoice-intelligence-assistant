using System.IO;
using ClosedXML.Excel;
using InvoiceIntelligence.Core.Processor;

namespace InvoiceIntelligence.Core.Tests.Processor;

public class FolderAInvoiceRecordReaderTests
{
    [Fact]
    public void ReadFromFolder_ValidWorkbook_ReadsInvoiceRecords()
    {
        var folderPath = CreateTempDirectory();
        var filePath = Path.Combine(folderPath, "sample.xlsx");
        CreateWorkbook(filePath);

        try
        {
            var reader = new FolderAInvoiceRecordReader();
            var records = reader.ReadFromFolder(folderPath);

            Assert.Equal(2, records.Count);
            Assert.Equal("INV-001", records[0].InvoiceNumber);
            Assert.Equal("12345678", records[0].TaxId);
            Assert.Equal("辦公文具", records[0].ItemName);
            Assert.Equal(1000m, records[0].NetAmount);
            Assert.Equal("sample.xlsx", records[0].SourceFileName);
        }
        finally
        {
            DeleteDirectory(folderPath);
        }
    }

    [Fact]
    public void ReadFromFolder_MissingRequiredHeader_ThrowsInvalidDataException()
    {
        var folderPath = CreateTempDirectory();
        var filePath = Path.Combine(folderPath, "invalid.xlsx");
        CreateWorkbookMissingRequiredColumn(filePath);

        try
        {
            var reader = new FolderAInvoiceRecordReader();

            Assert.Throws<InvalidDataException>(() => reader.ReadFromFolder(folderPath));
        }
        finally
        {
            DeleteDirectory(folderPath);
        }
    }

    private static void CreateWorkbook(string filePath)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("RawInvoices");

        sheet.Cell(1, 1).Value = "統編";
        sheet.Cell(1, 2).Value = "公司名稱";
        sheet.Cell(1, 3).Value = "發票號碼";
        sheet.Cell(1, 4).Value = "發票日期";
        sheet.Cell(1, 5).Value = "廠商統編";
        sheet.Cell(1, 6).Value = "品名";
        sheet.Cell(1, 7).Value = "未稅金額";
        sheet.Cell(1, 8).Value = "稅額";
        sheet.Cell(1, 9).Value = "含稅金額";

        sheet.Cell(2, 1).Value = "12345678";
        sheet.Cell(2, 2).Value = "Alpha Co";
        sheet.Cell(2, 3).Value = "INV-001";
        sheet.Cell(2, 4).Value = "2026-01-15";
        sheet.Cell(2, 5).Value = "87654321";
        sheet.Cell(2, 6).Value = "辦公文具";
        sheet.Cell(2, 7).Value = 1000m;
        sheet.Cell(2, 8).Value = 50m;
        sheet.Cell(2, 9).Value = 1050m;

        sheet.Cell(3, 1).Value = "12345678";
        sheet.Cell(3, 2).Value = "Alpha Co";
        sheet.Cell(3, 3).Value = "INV-002";
        sheet.Cell(3, 4).Value = "2026-01-16";
        sheet.Cell(3, 5).Value = "87654322";
        sheet.Cell(3, 6).Value = "客戶招待餐敘";
        sheet.Cell(3, 7).Value = 2000m;
        sheet.Cell(3, 8).Value = 100m;
        sheet.Cell(3, 9).Value = 2100m;

        workbook.SaveAs(filePath);
    }

    private static void CreateWorkbookMissingRequiredColumn(string filePath)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("RawInvoices");

        sheet.Cell(1, 1).Value = "統編";
        sheet.Cell(1, 2).Value = "公司名稱";
        sheet.Cell(1, 3).Value = "發票號碼";
        sheet.Cell(1, 4).Value = "發票日期";
        sheet.Cell(1, 5).Value = "廠商統編";
        sheet.Cell(1, 6).Value = "未稅金額";
        sheet.Cell(1, 7).Value = "稅額";
        sheet.Cell(1, 8).Value = "含稅金額";

        sheet.Cell(2, 1).Value = "12345678";
        sheet.Cell(2, 2).Value = "Alpha Co";
        sheet.Cell(2, 3).Value = "INV-001";
        sheet.Cell(2, 4).Value = "2026-01-15";
        sheet.Cell(2, 5).Value = "87654321";
        sheet.Cell(2, 6).Value = 1000m;
        sheet.Cell(2, 7).Value = 50m;
        sheet.Cell(2, 8).Value = 1050m;

        workbook.SaveAs(filePath);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"invoice-processor-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, true);
        }
    }
}