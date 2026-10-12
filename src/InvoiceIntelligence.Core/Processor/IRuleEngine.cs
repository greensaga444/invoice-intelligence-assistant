namespace InvoiceIntelligence.Core.Processor;

public interface IRuleEngine
{
    InvoiceDecision? TryResolve(InvoiceAnalysisRecord record);
}
