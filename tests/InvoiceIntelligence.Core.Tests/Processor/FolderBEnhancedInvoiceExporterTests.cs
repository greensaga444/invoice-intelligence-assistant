using System.IO;
using ClosedXML.Excel;
using InvoiceIntelligence.Core.Processor;

namespace InvoiceIntelligence.Core.Tests.Processor;

public class FolderBEnhancedInvoiceExporterTests
{
    [Fact]
    public void Export_ValidDecisionResults_WritesTableThreeAndTableFourWorkbooks()
    {
        var folderBPath = CreateTempDirectory();
        var results = new[]
        {
            CreateResult(
                sourceFileName: "raw-a-invoice.xlsx",
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
                ]),
            CreateResult(
                sourceFileName: "raw-a-allwance.xlsx",
                invoiceNumber: "ALW-001",
                itemName: "折讓測試",
                invoiceDate: new DateOnly(2026, 1, 15),
                deductibleStatus: "不可抵稅",
                deductionReason: "折讓示意資料。",
                accountingCategory: "交際費",
                confidenceScore: 0.88m,
                references: [])
        };

        var exporter = new FolderBEnhancedInvoiceExporter();

        try
        {
            var exportResult = exporter.Export(
                results,
                folderBPath,
                "{taxId}_{companyName}_{dateFrom}_{dateTo}.xlsx",
                new DateTimeOffset(2026, 1, 31, 8, 30, 0, TimeSpan.Zero));

            Assert.Equal(2, exportResult.ExportedFileCount);
            Assert.Equal(2, exportResult.ExportedRecordCount);
            Assert.Equal(2, exportResult.Files.Count);

            var tableThreePath = exportResult.Files
                .Single(file => Path.GetFileName(file.OutputFilePath).Contains("SalesInvoiceDetail-T3", StringComparison.Ordinal))
                .OutputFilePath;
            var tableFourPath = exportResult.Files
                .Single(file => Path.GetFileName(file.OutputFilePath).Contains("PurchaseVoucherDetail-T4", StringComparison.Ordinal))
                .OutputFilePath;

            Assert.True(File.Exists(tableThreePath));
            Assert.True(File.Exists(tableFourPath));

            using (var workbook = new XLWorkbook(tableThreePath))
            {
                var sheet = workbook.Worksheet(1);

                Assert.Equal("銷項發票明細檔(表三)", sheet.Name);
                Assert.Equal("送件年月 [1]", sheet.Cell(1, 1).GetString());
                Assert.Equal("發票號碼 [5]", sheet.Cell(1, 6).GetString());
                Assert.Equal("INV-001", sheet.Cell(2, 6).GetString());
                Assert.Equal(1000m, sheet.Cell(2, 8).GetValue<decimal>());
                Assert.Equal("5", sheet.Cell(2, 9).GetString());
                Assert.Equal(50m, sheet.Cell(2, 10).GetValue<decimal>());
            }

            using (var workbook = new XLWorkbook(tableFourPath))
            {
                var sheet = workbook.Worksheet(1);

                Assert.Equal("進項憑證明細檔(表四)", sheet.Name);
                Assert.Equal("送件年月 [1]", sheet.Cell(1, 1).GetString());
                Assert.Equal("發票號碼&繳納證號碼 [16]", sheet.Cell(1, 19).GetString());
                Assert.Equal("ALW-001", sheet.Cell(2, 19).GetString());
                Assert.Equal("N", sheet.Cell(2, 8).GetString());
                Assert.Equal(1000m, sheet.Cell(2, 11).GetValue<decimal>());
            }
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
                        sourceFileName: "source-invoice.xlsx",
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
            Assert.Equal("12345678_Alpha Co_20260210120544-SalesInvoiceDetail-T3.xlsx", outputFileName);
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
                sourceFileName: "raw-a-invoice.xlsx",
                invoiceNumber: "INV-A01",
                itemName: "項目A",
                invoiceDate: new DateOnly(2026, 3, 1),
                deductibleStatus: "可抵稅",
                deductionReason: "規則命中。",
                accountingCategory: "辦公用品費",
                confidenceScore: 0.95m,
                references: []),
            CreateResult(
                sourceFileName: "raw-b-invoice.xlsx",
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
            Assert.Contains("12345678_20260301-SalesInvoiceDetail-T3.xlsx", outputFileNames);
            Assert.Contains("12345678_20260301-SalesInvoiceDetail-T3-2.xlsx", outputFileNames);
        }
        finally
        {
            DeleteDirectory(folderBPath);
        }
    }

    [Fact]
    public void Export_CustomOutputLabels_UsesConfiguredSuffixes()
    {
        var folderBPath = CreateTempDirectory();
        var timestamp = new DateTimeOffset(2026, 4, 10, 10, 30, 0, TimeSpan.Zero);
        var exporter = new FolderBEnhancedInvoiceExporter();

        var results = new[]
        {
            CreateResult(
                sourceFileName: "source-invoice.xlsx",
                invoiceNumber: "INV-CUSTOM",
                itemName: "測試項目",
                invoiceDate: new DateOnly(2026, 4, 1),
                deductibleStatus: "可抵稅",
                deductionReason: "測試",
                accountingCategory: "辦公用品費",
                confidenceScore: 0.90m,
                references: []),
            CreateResult(
                sourceFileName: "source-allowance.xlsx",
                invoiceNumber: "ALW-CUSTOM",
                itemName: "折讓測試",
                invoiceDate: new DateOnly(2026, 4, 1),
                deductibleStatus: "不可抵稅",
                deductionReason: "測試",
                accountingCategory: "交際費",
                confidenceScore: 0.90m,
                references: [])
        };

        try
        {
            var exportResult = exporter.Export(
                results,
                folderBPath,
                "{taxId}_{dateFrom}.xlsx",
                timestamp,
                invoiceOutputLabel: "InvoiceReadable",
                allowanceOutputLabel: "AllowanceReadable");

            var outputFileNames = exportResult.Files
                .Select(file => Path.GetFileName(file.OutputFilePath))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            Assert.Contains("12345678_20260401-InvoiceReadable.xlsx", outputFileNames);
            Assert.Contains("12345678_20260401-AllowanceReadable.xlsx", outputFileNames);
        }
        finally
        {
            DeleteDirectory(folderBPath);
        }
    }

    [Fact]
    public void Export_SalesRecordWithZeroTax_WritesAsteriskTaxTypeAndExemptAmount()
    {
        var folderBPath = CreateTempDirectory();
        var exporter = new FolderBEnhancedInvoiceExporter();

        try
        {
            var exportResult = exporter.Export(
            [
                CreateResult(
                    sourceFileName: "source-invoice.xlsx",
                    invoiceNumber: "INV-ZERO-TAX",
                    itemName: "免稅銷售測試",
                    invoiceDate: new DateOnly(2026, 5, 1),
                    deductibleStatus: "可抵稅",
                    deductionReason: "測試",
                    accountingCategory: "營業收入",
                    confidenceScore: 0.90m,
                    references: [],
                    netAmount: 1000m,
                    taxAmount: 0m,
                    totalAmount: 1000m)
            ],
                folderBPath,
                "{taxId}_{dateFrom}.xlsx",
                new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero));

            var salesPath = exportResult.Files.Single().OutputFilePath;
            using var workbook = new XLWorkbook(salesPath);
            var sheet = workbook.Worksheet(1);

            Assert.Equal("*", sheet.Cell(2, 9).GetString());
            Assert.Equal(1000m, sheet.Cell(2, 17).GetValue<decimal>());
        }
        finally
        {
            DeleteDirectory(folderBPath);
        }
    }

    [Fact]
    public void Export_AllowanceRecord_MapsDeductibleFlagAndVoucherCategoryBranches()
    {
        var folderBPath = CreateTempDirectory();
        var exporter = new FolderBEnhancedInvoiceExporter();

        var results = new[]
        {
            CreateResult(
                sourceFileName: "source-allowance.xlsx",
                invoiceNumber: "ALW-001",
                itemName: "固定資產採購",
                invoiceDate: new DateOnly(2026, 6, 1),
                deductibleStatus: "可抵稅",
                deductionReason: "測試",
                accountingCategory: "固定資產",
                confidenceScore: 0.9m,
                references: []),
            CreateResult(
                sourceFileName: "source-allowance.xlsx",
                invoiceNumber: "ALW-002",
                itemName: "一般支出",
                invoiceDate: new DateOnly(2026, 6, 1),
                deductibleStatus: "待確認",
                deductionReason: "測試",
                accountingCategory: "其他",
                confidenceScore: 0.9m,
                references: [])
        };

        try
        {
            var exportResult = exporter.Export(
                results,
                folderBPath,
                "{taxId}_{dateFrom}.xlsx",
                new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));

            var purchasePath = exportResult.Files
                .Single(file => Path.GetFileName(file.OutputFilePath).Contains("PurchaseVoucherDetail-T4", StringComparison.Ordinal));

            using var workbook = new XLWorkbook(purchasePath.OutputFilePath);
            var sheet = workbook.Worksheet(1);

            Assert.Equal("Y", sheet.Cell(2, 8).GetString());
            Assert.Equal("3", sheet.Cell(2, 10).GetString());
            Assert.Equal("N", sheet.Cell(3, 8).GetString());
            Assert.Equal("1", sheet.Cell(3, 10).GetString());
            Assert.Equal("115", sheet.Cell(2, 3).GetString());
            Assert.Equal("1150601", sheet.Cell(2, 14).GetString());
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
        IReadOnlyList<RetrievedReferenceCase> references,
        decimal netAmount = 1000m,
        decimal taxAmount = 50m,
        decimal totalAmount = 1050m)
    {
        var record = new InvoiceAnalysisRecord(
            InvoiceNumber: invoiceNumber,
            TaxId: "12345678",
            CompanyName: "Alpha Co",
            VendorTaxId: "87654321",
            ItemName: itemName,
            InvoiceDate: invoiceDate,
            NetAmount: netAmount,
            TaxAmount: taxAmount,
            TotalAmount: totalAmount,
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