using InvoiceIntelligence.Core.Downloader;

namespace InvoiceIntelligence.Core.Tests.Downloader;

public class LocalMofAuthenticatorTests
{
    [Fact]
    public void Authenticate_ValidCredential_ReturnsSuccess()
    {
        var authenticator = new LocalMofAuthenticator();
        var credential = new MofCredential("mof-user", "api-key-1234", "password123");

        var result = authenticator.Authenticate(credential);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.SessionToken);
        Assert.NotNull(result.ExpiresAt);
    }

    [Fact]
    public void Authenticate_PlaceholderAccount_ReturnsFailure()
    {
        var authenticator = new LocalMofAuthenticator();
        var credential = new MofCredential("replace-with-mof-account", "api-key-1234", "password123");

        var result = authenticator.Authenticate(credential);

        Assert.False(result.IsSuccess);
        Assert.Contains("帳號", result.Message);
    }

    [Fact]
    public void Authenticate_EmptyPassword_ReturnsFailure()
    {
        var authenticator = new LocalMofAuthenticator();
        var credential = new MofCredential("mof-user", "api-key-1234", string.Empty);

        var result = authenticator.Authenticate(credential);

        Assert.False(result.IsSuccess);
        Assert.Contains("密碼", result.Message);
    }
}
