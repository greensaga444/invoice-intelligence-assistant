using System.Text.RegularExpressions;
using InvoiceIntelligence.Core.Configuration;

namespace InvoiceIntelligence.Core.Tests.Configuration;

public class NamingRuleValidatorTests
{
    [Fact]
    public void Validate_NullOrWhitespace_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => NamingRuleValidator.Validate(null!));
        Assert.Throws<FormatException>(() => NamingRuleValidator.Validate(string.Empty));
        Assert.Throws<FormatException>(() => NamingRuleValidator.Validate("   "));
    }

    [Fact]
    public void Validate_WithoutPlaceholders_ThrowsFormatException()
    {
        var exception = Assert.Throws<FormatException>(() => NamingRuleValidator.Validate("invoice-output-file"));

        Assert.Contains("placeholder", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WithKnownPlaceholdersInDifferentCase_DoesNotThrow()
    {
        var namingRule = "{TaxId}_{COMPANYNAME}_{DateFrom}_{dateto}_{TIMESTAMP}.xlsx";

        var exception = Record.Exception(() => NamingRuleValidator.Validate(namingRule));

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WithUnknownPlaceholder_ThrowsFormatException()
    {
        var exception = Assert.Throws<FormatException>(() => NamingRuleValidator.Validate("{taxId}_{unknownField}.xlsx"));

        Assert.Contains("unknownField", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WithDuplicateUnknownPlaceholderInDifferentCase_ReportsOnlyOnce()
    {
        var exception = Assert.Throws<FormatException>(() => NamingRuleValidator.Validate("{bad}_{BAD}_{taxId}.xlsx"));

        var occurrences = Regex.Matches(exception.Message, "bad", RegexOptions.IgnoreCase).Count;
        Assert.Equal(1, occurrences);
    }
}