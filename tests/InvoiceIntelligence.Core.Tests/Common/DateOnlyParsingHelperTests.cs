using InvoiceIntelligence.Core.Common;

namespace InvoiceIntelligence.Core.Tests.Common;

public class DateOnlyParsingHelperTests
{
    [Theory]
    [InlineData("2026-01-31", 2026, 1, 31)]
    [InlineData("2026/01/31", 2026, 1, 31)]
    public void TryParseDate_WhenInputIsValid_ReturnsTrue(string input, int year, int month, int day)
    {
        var success = DateOnlyParsingHelper.TryParseDate(input, out var value);

        Assert.True(success);
        Assert.Equal(new DateOnly(year, month, day), value);
    }

    [Fact]
    public void TryParseDate_WhenInputIsInvalid_ReturnsFalse()
    {
        var success = DateOnlyParsingHelper.TryParseDate("not-a-date", out _);

        Assert.False(success);
    }
}
