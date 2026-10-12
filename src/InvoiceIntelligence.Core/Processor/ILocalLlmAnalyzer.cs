namespace InvoiceIntelligence.Core.Processor;

public interface ILocalLlmAnalyzer
{
    InvoiceDecision Analyze(InvoiceAnalysisRecord record, IReadOnlyList<RetrievedReferenceCase> referenceCases);
}
