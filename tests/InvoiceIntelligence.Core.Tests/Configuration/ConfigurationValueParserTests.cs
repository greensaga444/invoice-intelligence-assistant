using System.Globalization;
using InvoiceIntelligence.Core.Configuration;

namespace InvoiceIntelligence.Core.Tests.Configuration;

public class ConfigurationValueParserTests
{
    [Fact]
    public void TryParseLowConfidenceThreshold_ValidInvariantValue_RoundsToTwoDecimals()
    {
        var success = ConfigurationValueParser.TryParseLowConfidenceThreshold("0.755", out var threshold);

        Assert.True(success);
        Assert.Equal(0.76m, threshold);
    }

    [Fact]
    public void TryParseLowConfidenceThreshold_TrimmedValue_ReturnsParsedThreshold()
    {
        var text = $"  {0.74m.ToString(CultureInfo.InvariantCulture)}  ";

        var success = ConfigurationValueParser.TryParseLowConfidenceThreshold(text, out var threshold);

        Assert.True(success);
        Assert.Equal(0.74m, threshold);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-a-number")]
    public void TryParseLowConfidenceThreshold_InvalidValue_ReturnsFalse(string? value)
    {
        var success = ConfigurationValueParser.TryParseLowConfidenceThreshold(value, out var threshold);

        Assert.False(success);
        Assert.Equal(0m, threshold);
    }

    [Theory]
    [InlineData("-0.01", -0.01)]
    [InlineData("1.01", 1.01)]
    public void TryParseLowConfidenceThreshold_OutOfRangeValue_ReturnsFalseAndPreservesParsedThreshold(string value, double expected)
    {
        var success = ConfigurationValueParser.TryParseLowConfidenceThreshold(value, out var threshold);

        Assert.False(success);
        Assert.Equal((decimal)expected, threshold);
    }
}
