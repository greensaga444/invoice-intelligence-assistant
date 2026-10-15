using InvoiceIntelligence.Core.Processor;

namespace InvoiceIntelligence.Core.Tests.Processor;

public class DecisionStageExtensionsTests
{
    [Theory]
    [InlineData(DecisionStage.RuleEngine, "規則引擎")]
    [InlineData(DecisionStage.CaseCache, "案例快取")]
    [InlineData(DecisionStage.Llm, "本地 LLM")]
    public void ToDisplayName_KnownStages_ReturnsExpectedDisplayName(
        DecisionStage stage,
        string expectedDisplayName)
    {
        var displayName = stage.ToDisplayName();

        Assert.Equal(expectedDisplayName, displayName);
    }

    [Fact]
    public void ToDisplayName_UnknownStage_ReturnsFallbackText()
    {
        var displayName = ((DecisionStage)999).ToDisplayName();

        Assert.Equal("未知來源", displayName);
    }
}
