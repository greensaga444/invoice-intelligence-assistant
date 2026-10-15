using InvoiceIntelligence.Core.Downloader;

namespace InvoiceIntelligence.Core.Tests.Downloader;

public class LocalMofAuthenticatorTests
{
    [Fact]
    public void Authenticate_NullCredential_ThrowsArgumentNullException()
    {
        var authenticator = new LocalMofAuthenticator();

        Assert.Throws<ArgumentNullException>(() => authenticator.Authenticate(null!));
    }

    [Fact]
    public void Authenticate_ValidCredential_ReturnsSuccess()
    {
        var authenticator = new LocalMofAuthenticator();
        var credential = new MofCredential("mof-user", "api-key-1234", "password123");
        var now = DateTimeOffset.UtcNow;

        var result = authenticator.Authenticate(credential);

        Assert.True(result.IsSuccess);
        Assert.StartsWith("mof-local-", result.SessionToken, StringComparison.Ordinal);
        Assert.NotNull(result.ExpiresAt);
        Assert.InRange(result.ExpiresAt!.Value, now.AddMinutes(29), now.AddMinutes(31));
    }

    [Fact]
    public void Authenticate_PlaceholderAccount_ReturnsFailure()
    {
        var authenticator = new LocalMofAuthenticator();
        var credential = new MofCredential("replace-with-mof-account", "api-key-1234", "password123");

        var result = authenticator.Authenticate(credential);

        Assert.False(result.IsSuccess);
        Assert.Contains("帳號", result.Message);
        Assert.Null(result.SessionToken);
        Assert.Null(result.ExpiresAt);
    }

    [Fact]
    public void Authenticate_PlaceholderApiKey_ReturnsFailure()
    {
        var authenticator = new LocalMofAuthenticator();
        var credential = new MofCredential("mof-user", "replace-with-app-key", "password123");

        var result = authenticator.Authenticate(credential);

        Assert.False(result.IsSuccess);
        Assert.Contains("API", result.Message, StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public void Authenticate_PasswordTooShort_ReturnsFailure()
    {
        var authenticator = new LocalMofAuthenticator();
        var credential = new MofCredential("mof-user", "api-key-1234", "1234567");

        var result = authenticator.Authenticate(credential);

        Assert.False(result.IsSuccess);
        Assert.Contains("8", result.Message, StringComparison.Ordinal);
    }
}
