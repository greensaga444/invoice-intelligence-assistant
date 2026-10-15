using ClosedXML.Excel;

namespace InvoiceIntelligence.Core.Processor;

internal interface IFolderAWorksheetRecordReader
{
    bool CanRead(XLWorkbook workbook, IXLWorksheet firstWorksheet);

    IReadOnlyList<InvoiceAnalysisRecord> Read(XLWorkbook workbook, string filePath);
}
