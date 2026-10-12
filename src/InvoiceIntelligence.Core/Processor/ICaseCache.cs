namespace InvoiceIntelligence.Core.Processor;

public interface ICaseCache
{
    InvoiceDecision? TryResolve(InvoiceAnalysisRecord record);
}
