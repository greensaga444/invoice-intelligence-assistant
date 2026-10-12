namespace InvoiceIntelligence.Core.Processor;

public interface IRagRetriever
{
    IReadOnlyList<RetrievedReferenceCase> Retrieve(InvoiceAnalysisRecord record, int maxResults);
}
