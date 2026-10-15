using InvoiceIntelligence.Core.Configuration;

namespace InvoiceIntelligence.Core.Processor;

public sealed class FolderBEnhancedInvoiceExporter
{
    private readonly SalesDetailWorkbookWriter _salesWorkbookWriter = new();
    private readonly PurchaseVoucherWorkbookWriter _purchaseWorkbookWriter = new();

    public FolderBExportResult Export(
        IReadOnlyCollection<InvoiceDecisionResult> decisionResults,
        string folderBPath,
        string outputNamingRule,
        DateTimeOffset? exportTimestamp = null,
        string? invoiceOutputLabel = null,
        string? allowanceOutputLabel = null,
        string invoiceFileSuffix = "invoice",
        string allowanceFileSuffix = "allowance")
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
        var resolvedInvoiceOutputLabel = ResolveOutputLabel(invoiceOutputLabel, ConfigurationDefaults.InvoiceOutputLabel);
        var resolvedAllowanceOutputLabel = ResolveOutputLabel(allowanceOutputLabel, ConfigurationDefaults.AllowanceOutputLabel);
        var groupedResults = decisionResults
            .GroupBy(
                item => FolderAFileCategoryHelper.BuildGroupKey(
                    item.Record.SourceFileName,
                    invoiceFileSuffix,
                    allowanceFileSuffix),
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var outputFiles = new List<FolderBExportedFileResult>(groupedResults.Length * 2);

        foreach (var group in groupedResults)
        {
            var items = group
                .OrderBy(item => item.Record.InvoiceDate)
                .ThenBy(item => item.Record.InvoiceNumber, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (items.Length == 0)
            {
                continue;
            }

            var firstRecord = items[0].Record;
            var dateFrom = items.Min(item => item.Record.InvoiceDate);
            var dateTo = items.Max(item => item.Record.InvoiceDate);

            var outputFileName = OutputFileNameHelper.BuildFileName(
                namingRule: outputNamingRule,
                taxId: firstRecord.TaxId,
                companyName: firstRecord.CompanyName,
                dateFrom: dateFrom,
                dateTo: dateTo,
                timestamp: timestamp,
                extension: ".xlsx");

            var invoiceItems = items
                .Where(item =>
                {
                    var category = FolderAFileCategoryHelper.GetCategory(
                        item.Record.SourceFileName,
                        invoiceFileSuffix,
                        allowanceFileSuffix);
                    return category is FolderAFileCategory.Invoice or FolderAFileCategory.Unknown;
                })
                .ToArray();

            var allowanceItems = items
                .Where(item =>
                    FolderAFileCategoryHelper.GetCategory(
                        item.Record.SourceFileName,
                        invoiceFileSuffix,
                        allowanceFileSuffix) == FolderAFileCategory.Allowance)
                .ToArray();

            var sourceGroupName = string.IsNullOrWhiteSpace(group.Key)
                ? firstRecord.SourceFileName
                : group.Key;

            if (invoiceItems.Length > 0)
            {
                var outputPath = BuildTypedOutputPath(folderBPath, outputFileName, resolvedInvoiceOutputLabel);
                _salesWorkbookWriter.Write(outputPath, invoiceItems);
                outputFiles.Add(new FolderBExportedFileResult(
                    SourceFileName: $"{sourceGroupName}-invoice",
                    OutputFilePath: outputPath,
                    RecordCount: invoiceItems.Length));
            }

            if (allowanceItems.Length > 0)
            {
                var outputPath = BuildTypedOutputPath(folderBPath, outputFileName, resolvedAllowanceOutputLabel);
                _purchaseWorkbookWriter.Write(outputPath, allowanceItems);
                outputFiles.Add(new FolderBExportedFileResult(
                    SourceFileName: $"{sourceGroupName}-allowance",
                    OutputFilePath: outputPath,
                    RecordCount: allowanceItems.Length));
            }
        }

        return new FolderBExportResult(outputFiles);
    }

    private static string BuildTypedOutputPath(string folderPath, string baseFileName, string tableSuffix)
    {
        var extension = Path.GetExtension(baseFileName);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseFileName);
        var typedFileName = $"{fileNameWithoutExtension}-{SanitizeOutputLabel(tableSuffix)}{extension}";
        return OutputFileNameHelper.EnsureUniquePath(Path.Combine(folderPath, typedFileName));
    }

    private static string ResolveOutputLabel(string? configuredLabel, string fallback)
    {
        return string.IsNullOrWhiteSpace(configuredLabel)
            ? fallback
            : configuredLabel.Trim();
    }

    private static string SanitizeOutputLabel(string label)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var buffer = label.ToCharArray();

        for (var index = 0; index < buffer.Length; index++)
        {
            if (invalidCharacters.Contains(buffer[index]))
            {
                buffer[index] = '_';
            }
        }

        return new string(buffer);
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