using InvoiceIntelligence.Core.Configuration;

namespace InvoiceIntelligence.Core.Tests.Configuration;

public class MofApiCredentialResolverTests
{
    [Fact]
    public void ResolveAppId_WhenPrimaryExists_ReturnsTrimmedPrimary()
    {
        var value = MofApiCredentialResolver.ResolveAppId(" app-id ", "app-key");

        Assert.Equal("app-id", value);
    }

    [Fact]
    public void ResolveAppKey_WhenPrimaryMissing_ReturnsTrimmedFallback()
    {
        var value = MofApiCredentialResolver.ResolveAppKey(null, " app-id ");

        Assert.Equal("app-id", value);
    }

    [Fact]
    public void ResolvePrimaryOrFallback_WhenBothMissing_ReturnsEmptyString()
    {
        var value = MofApiCredentialResolver.ResolvePrimaryOrFallback(" ", null);

        Assert.Equal(string.Empty, value);
    }
}
