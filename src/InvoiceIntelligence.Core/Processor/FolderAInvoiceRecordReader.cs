using ClosedXML.Excel;

namespace InvoiceIntelligence.Core.Processor;

public sealed class FolderAInvoiceRecordReader
{
    private readonly IReadOnlyList<IFolderAWorksheetRecordReader> _worksheetReaders;

    public FolderAInvoiceRecordReader()
        : this([new LegacyFolderAWorksheetRecordReader(), new OfficialFolderAWorksheetRecordReader()])
    {
    }

    internal FolderAInvoiceRecordReader(IReadOnlyList<IFolderAWorksheetRecordReader> worksheetReaders)
    {
        _worksheetReaders = worksheetReaders ?? throw new ArgumentNullException(nameof(worksheetReaders));
    }

    public IReadOnlyList<InvoiceAnalysisRecord> ReadFromFolder(
        string folderAPath,
        string invoiceFileSuffix = "invoice",
        string allowanceFileSuffix = "allowance")
    {
        if (string.IsNullOrWhiteSpace(folderAPath))
        {
            throw new ArgumentException("資料夾 A 路徑不可為空白。", nameof(folderAPath));
        }

        if (!Directory.Exists(folderAPath))
        {
            return [];
        }

        var candidateFiles = Directory
            .EnumerateFiles(folderAPath, "*.xlsx", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var invoiceFiles = candidateFiles
            .Where(path => FolderAFileCategoryHelper.IsPreferredSourceFile(path, invoiceFileSuffix, allowanceFileSuffix))
            .ToArray();

        if (invoiceFiles.Length == 0)
        {
            invoiceFiles = candidateFiles;
        }

        var allRecords = new List<InvoiceAnalysisRecord>();

        foreach (var filePath in invoiceFiles)
        {
            allRecords.AddRange(ReadFromFile(filePath));
        }

        return allRecords;
    }

    private IReadOnlyList<InvoiceAnalysisRecord> ReadFromFile(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);
        var firstWorksheet = workbook.Worksheets.FirstOrDefault();
        if (firstWorksheet is null)
        {
            throw new InvalidDataException($"發票活頁簿不包含任何工作表：{filePath}");
        }

        foreach (var worksheetReader in _worksheetReaders)
        {
            if (worksheetReader.CanRead(workbook, firstWorksheet))
            {
                return worksheetReader.Read(workbook, filePath);
            }
        }

        throw new InvalidDataException($"發票工作表格式不支援：{Path.GetFileName(filePath)}");
    }
}
