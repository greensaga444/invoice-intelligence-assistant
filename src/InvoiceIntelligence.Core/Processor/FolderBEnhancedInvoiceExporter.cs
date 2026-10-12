using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using InvoiceIntelligence.Core.Configuration;

namespace InvoiceIntelligence.Core.Processor;

public sealed class FolderBEnhancedInvoiceExporter
{
    public FolderBExportResult Export(
        IReadOnlyCollection<InvoiceDecisionResult> decisionResults,
        string folderBPath,
        string outputNamingRule,
        DateTimeOffset? exportTimestamp = null)
    {
        ArgumentNullException.ThrowIfNull(decisionResults);

        if (string.IsNullOrWhiteSpace(folderBPath))
        {
            throw new ArgumentException("資料夾 B 路徑不可為空白。", nameof(folderBPath));
        }

        NamingRuleValidator.Validate(outputNamingRule);

        if (decisionResults.Count == 0)
        {
            return new FolderBExportResult([]);
        }

        Directory.CreateDirectory(folderBPath);

        var timestamp = exportTimestamp ?? DateTimeOffset.UtcNow;
        var groupedResults = decisionResults
            .GroupBy(item => item.Record.SourceFileName, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var outputFiles = new List<FolderBExportedFileResult>(groupedResults.Length);

        foreach (var group in groupedResults)
        {
            var items = group
                .OrderBy(item => item.Record.InvoiceDate)
                .ThenBy(item => item.Record.InvoiceNumber, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var firstRecord = items[0].Record;
            var dateFrom = items.Min(item => item.Record.InvoiceDate);
            var dateTo = items.Max(item => item.Record.InvoiceDate);

            var outputFileName = BuildFileName(
                outputNamingRule,
                firstRecord.TaxId,
                firstRecord.CompanyName,
                dateFrom,
                dateTo,
                timestamp);

            var outputPath = EnsureUniquePath(Path.Combine(folderBPath, outputFileName));

            WriteWorkbook(outputPath, items);

            outputFiles.Add(new FolderBExportedFileResult(
                SourceFileName: group.Key,
                OutputFilePath: outputPath,
                RecordCount: items.Length));
        }

        return new FolderBExportResult(outputFiles);
    }

    private static string BuildFileName(
        string namingRule,
        string taxId,
        string companyName,
        DateOnly dateFrom,
        DateOnly dateTo,
        DateTimeOffset timestamp)
    {
        var fileName = namingRule
            .Replace("{taxId}", SanitizeSegment(taxId), StringComparison.OrdinalIgnoreCase)
            .Replace("{companyName}", SanitizeSegment(companyName), StringComparison.OrdinalIgnoreCase)
            .Replace("{dateFrom}", dateFrom.ToString("yyyyMMdd", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{dateTo}", dateTo.ToString("yyyyMMdd", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{timestamp}", timestamp.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);

        if (!fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            fileName += ".xlsx";
        }

        return fileName;
    }

    private static string EnsureUniquePath(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return filePath;
        }

        var directory = Path.GetDirectoryName(filePath)
            ?? throw new InvalidOperationException("資料夾 B 輸出路徑缺少目錄資訊。");
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);

        for (var index = 2; index <= 9999; index++)
        {
            var candidatePath = Path.Combine(directory, $"{fileNameWithoutExtension}-{index}{extension}");
            if (!File.Exists(candidatePath))
            {
                return candidatePath;
            }
        }

        throw new IOException("無法產生唯一的資料夾 B 輸出檔名。");
    }

    private static string SanitizeSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "未提供";
        }

        var invalidCharacters = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);

        foreach (var character in value.Trim())
        {
            if (invalidCharacters.Contains(character))
            {
                builder.Append('_');
                continue;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    private static void WriteWorkbook(string outputPath, IReadOnlyList<InvoiceDecisionResult> items)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("增強發票");

        worksheet.Cell(1, 1).Value = "統編";
        worksheet.Cell(1, 2).Value = "公司名稱";
        worksheet.Cell(1, 3).Value = "發票號碼";
        worksheet.Cell(1, 4).Value = "發票日期";
        worksheet.Cell(1, 5).Value = "廠商統編";
        worksheet.Cell(1, 6).Value = "品名";
        worksheet.Cell(1, 7).Value = "未稅金額";
        worksheet.Cell(1, 8).Value = "稅額";
        worksheet.Cell(1, 9).Value = "含稅金額";
        worksheet.Cell(1, 10).Value = "AI_可抵稅";
        worksheet.Cell(1, 11).Value = "AI_判定原因";
        worksheet.Cell(1, 12).Value = "AI_建議科目";
        worksheet.Cell(1, 13).Value = "AI_信心度";
        worksheet.Cell(1, 14).Value = "AI_參考案例";

        for (var index = 0; index < items.Count; index++)
        {
            var rowNumber = index + 2;
            var item = items[index];
            var record = item.Record;
            var decision = item.Decision;

            worksheet.Cell(rowNumber, 1).Value = record.TaxId;
            worksheet.Cell(rowNumber, 2).Value = record.CompanyName;
            worksheet.Cell(rowNumber, 3).Value = record.InvoiceNumber;
            worksheet.Cell(rowNumber, 4).Value = record.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            worksheet.Cell(rowNumber, 5).Value = record.VendorTaxId;
            worksheet.Cell(rowNumber, 6).Value = record.ItemName;
            worksheet.Cell(rowNumber, 7).Value = record.NetAmount;
            worksheet.Cell(rowNumber, 8).Value = record.TaxAmount;
            worksheet.Cell(rowNumber, 9).Value = record.TotalAmount;

            worksheet.Cell(rowNumber, 10).Value = decision.DeductibleStatus;
            worksheet.Cell(rowNumber, 11).Value = decision.DeductionReason;
            worksheet.Cell(rowNumber, 12).Value = decision.AccountingCategory;
            worksheet.Cell(rowNumber, 13).Value = decision.ConfidenceScore;
            worksheet.Cell(rowNumber, 14).Value = BuildReferenceCaseSummary(item.ReferenceCases);
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(outputPath);
    }

    private static string BuildReferenceCaseSummary(IReadOnlyCollection<RetrievedReferenceCase> referenceCases)
    {
        if (referenceCases.Count == 0)
        {
            return "-";
        }

        return string.Join(
            " | ",
            referenceCases.Select(reference =>
                $"{reference.CaseId}:{reference.DeductibleStatus}/{reference.AccountingCategory}({reference.SimilarityScore:0.00})"));
    }
}

public sealed record FolderBExportedFileResult(
    string SourceFileName,
    string OutputFilePath,
    int RecordCount);

public sealed record FolderBExportResult(IReadOnlyList<FolderBExportedFileResult> Files)
{
    public int ExportedFileCount => Files.Count;

    public int ExportedRecordCount => Files.Sum(file => file.RecordCount);
}