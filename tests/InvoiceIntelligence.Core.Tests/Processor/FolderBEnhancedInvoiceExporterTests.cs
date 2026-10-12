using System.IO;
using ClosedXML.Excel;
using InvoiceIntelligence.Core.Processor;

namespace InvoiceIntelligence.Core.Tests.Processor;

public class FolderBEnhancedInvoiceExporterTests
{
    [Fact]
    public void Export_ValidDecisionResults_WritesEnhancedWorkbookWithPreservedAndAiColumns()
    {
        var folderBPath = CreateTempDirectory();
        var results = new[]
        {
            CreateResult(
                sourceFileName: "raw-a.xlsx",
                invoiceNumber: "INV-001",
                itemName: "辦公文具",
                invoiceDate: new DateOnly(2026, 1, 15),
                deductibleStatus: "可抵稅",
                deductionReason: "符合辦公用品用途。",
                accountingCategory: "辦公用品費",
                confidenceScore: 0.91m,
                references:
                [
                    new RetrievedReferenceCase("KB-001", "文具案例", "可抵稅", "辦公用品費", 0.87m)
                ])
        };

        var exporter = new FolderBEnhancedInvoiceExporter();

        try
        {
            var exportResult = exporter.Export(
                results,
                folderBPath,
                "{taxId}_{companyName}_{dateFrom}_{dateTo}.xlsx",
                new DateTimeOffset(2026, 1, 31, 8, 30, 0, TimeSpan.Zero));

            Assert.Equal(1, exportResult.ExportedFileCount);
            Assert.Equal(1, exportResult.ExportedRecordCount);
            Assert.Single(exportResult.Files);

            var outputPath = exportResult.Files[0].OutputFilePath;
            Assert.True(File.Exists(outputPath));

            using var workbook = new XLWorkbook(outputPath);
            var sheet = workbook.Worksheet(1);

            Assert.Equal("統編", sheet.Cell(1, 1).GetString());
            Assert.Equal("公司名稱", sheet.Cell(1, 2).GetString());
            Assert.Equal("發票號碼", sheet.Cell(1, 3).GetString());
            Assert.Equal("發票日期", sheet.Cell(1, 4).GetString());
            Assert.Equal("廠商統編", sheet.Cell(1, 5).GetString());
            Assert.Equal("品名", sheet.Cell(1, 6).GetString());
            Assert.Equal("未稅金額", sheet.Cell(1, 7).GetString());
            Assert.Equal("稅額", sheet.Cell(1, 8).GetString());
            Assert.Equal("含稅金額", sheet.Cell(1, 9).GetString());
            Assert.Equal("AI_可抵稅", sheet.Cell(1, 10).GetString());
            Assert.Equal("AI_判定原因", sheet.Cell(1, 11).GetString());
            Assert.Equal("AI_建議科目", sheet.Cell(1, 12).GetString());
            Assert.Equal("AI_信心度", sheet.Cell(1, 13).GetString());
            Assert.Equal("AI_參考案例", sheet.Cell(1, 14).GetString());

            Assert.Equal("12345678", sheet.Cell(2, 1).GetString());
            Assert.Equal("Alpha Co", sheet.Cell(2, 2).GetString());
            Assert.Equal("INV-001", sheet.Cell(2, 3).GetString());
            Assert.Equal("2026-01-15", sheet.Cell(2, 4).GetString());
            Assert.Equal("87654321", sheet.Cell(2, 5).GetString());
            Assert.Equal("辦公文具", sheet.Cell(2, 6).GetString());
            Assert.Equal(1000m, sheet.Cell(2, 7).GetValue<decimal>());
            Assert.Equal(50m, sheet.Cell(2, 8).GetValue<decimal>());
            Assert.Equal(1050m, sheet.Cell(2, 9).GetValue<decimal>());
            Assert.Equal("可抵稅", sheet.Cell(2, 10).GetString());
            Assert.Equal("符合辦公用品用途。", sheet.Cell(2, 11).GetString());
            Assert.Equal("辦公用品費", sheet.Cell(2, 12).GetString());
            Assert.Equal(0.91m, sheet.Cell(2, 13).GetValue<decimal>());
            Assert.Equal("KB-001:可抵稅/辦公用品費(0.87)", sheet.Cell(2, 14).GetString());
        }
        finally
        {
            DeleteDirectory(folderBPath);
        }
    }

    [Fact]
    public void Export_WithTimestampPlaceholder_AppliesConfiguredNamingRule()
    {
        var folderBPath = CreateTempDirectory();
        var timestamp = new DateTimeOffset(2026, 2, 10, 12, 5, 44, TimeSpan.Zero);

        var exporter = new FolderBEnhancedInvoiceExporter();

        try
        {
            var exportResult = exporter.Export(
                [
                    CreateResult(
                        sourceFileName: "source.xlsx",
                        invoiceNumber: "INV-900",
                        itemName: "雲端服務",
                        invoiceDate: new DateOnly(2026, 2, 1),
                        deductibleStatus: "可抵稅",
                        deductionReason: "營業相關支出。",
                        accountingCategory: "資訊服務費",
                        confidenceScore: 0.88m,
                        references: [])
                ],
                folderBPath,
                "{taxId}_{companyName}_{timestamp}.xlsx",
                timestamp);

            Assert.Single(exportResult.Files);

            var outputFileName = Path.GetFileName(exportResult.Files[0].OutputFilePath);
            Assert.Equal("12345678_Alpha Co_20260210120544.xlsx", outputFileName);
        }
        finally
        {
            DeleteDirectory(folderBPath);
        }
    }

    [Fact]
    public void Export_TwoSourceFilesWithSameNamingResult_UsesUniqueFileNames()
    {
        var folderBPath = CreateTempDirectory();
        var timestamp = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

        var exporter = new FolderBEnhancedInvoiceExporter();
        var results = new[]
        {
            CreateResult(
                sourceFileName: "raw-a.xlsx",
                invoiceNumber: "INV-A01",
                itemName: "項目A",
                invoiceDate: new DateOnly(2026, 3, 1),
                deductibleStatus: "可抵稅",
                deductionReason: "規則命中。",
                accountingCategory: "辦公用品費",
                confidenceScore: 0.95m,
                references: []),
            CreateResult(
                sourceFileName: "raw-b.xlsx",
                invoiceNumber: "INV-B01",
                itemName: "項目B",
                invoiceDate: new DateOnly(2026, 3, 1),
                deductibleStatus: "可抵稅",
                deductionReason: "規則命中。",
                accountingCategory: "辦公用品費",
                confidenceScore: 0.95m,
                references: [])
        };

        try
        {
            var exportResult = exporter.Export(
                results,
                folderBPath,
                "{taxId}_{dateFrom}.xlsx",
                timestamp);

            Assert.Equal(2, exportResult.Files.Count);

            var outputFileNames = exportResult.Files
                .Select(file => Path.GetFileName(file.OutputFilePath))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            Assert.Equal(2, outputFileNames.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.Contains("12345678_20260301.xlsx", outputFileNames);
            Assert.Contains("12345678_20260301-2.xlsx", outputFileNames);
        }
        finally
        {
            DeleteDirectory(folderBPath);
        }
    }

    private static InvoiceDecisionResult CreateResult(
        string sourceFileName,
        string invoiceNumber,
        string itemName,
        DateOnly invoiceDate,
        string deductibleStatus,
        string deductionReason,
        string accountingCategory,
        decimal confidenceScore,
        IReadOnlyList<RetrievedReferenceCase> references)
    {
        var record = new InvoiceAnalysisRecord(
            InvoiceNumber: invoiceNumber,
            TaxId: "12345678",
            CompanyName: "Alpha Co",
            VendorTaxId: "87654321",
            ItemName: itemName,
            InvoiceDate: invoiceDate,
            NetAmount: 1000m,
            TaxAmount: 50m,
            TotalAmount: 1050m,
            SourceFileName: sourceFileName);

        var decision = new InvoiceDecision(
            DeductibleStatus: deductibleStatus,
            DeductionReason: deductionReason,
            AccountingCategory: accountingCategory,
            ConfidenceScore: confidenceScore);

        return new InvoiceDecisionResult(record, decision, DecisionStage.Llm, references);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"invoice-folder-b-tests-{Guid.NewGuid():N}");
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