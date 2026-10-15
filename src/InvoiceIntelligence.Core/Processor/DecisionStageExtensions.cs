namespace InvoiceIntelligence.Core.Processor;

public static class DecisionStageExtensions
{
    public static string ToDisplayName(this DecisionStage stage)
    {
        return stage switch
        {
            DecisionStage.RuleEngine => "規則引擎",
            DecisionStage.CaseCache => "案例快取",
            DecisionStage.Llm => "本地 LLM",
            _ => "未知來源"
        };
    }
}
