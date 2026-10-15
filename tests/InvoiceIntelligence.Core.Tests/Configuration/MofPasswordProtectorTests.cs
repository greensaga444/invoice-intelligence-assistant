using InvoiceIntelligence.Core.Configuration;

namespace InvoiceIntelligence.Core.Tests.Configuration;

public class MofPasswordProtectorTests
{
    [Fact]
    public void Encrypt_WhenPlainTextIsWhitespace_ReturnsEmptyString()
    {
        var encrypted = MofPasswordProtector.Encrypt(" ");

        Assert.Equal(string.Empty, encrypted);
    }

    [Fact]
    public void TryDecrypt_WhenEncryptedTextIsWhitespace_ReturnsFalse()
    {
        var success = MofPasswordProtector.TryDecrypt(" ", out var plainText);

        Assert.False(success);
        Assert.Equal(string.Empty, plainText);
    }

    [Fact]
    public void TryDecrypt_WhenEncryptedTextIsInvalidBase64_ReturnsFalse()
    {
        var success = MofPasswordProtector.TryDecrypt("not-base64", out var plainText);

        Assert.False(success);
        Assert.Equal(string.Empty, plainText);
    }

    [Fact]
    public void TryDecrypt_WhenCipherIsRandomBytes_ReturnsFalse()
    {
        var randomCipher = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        var success = MofPasswordProtector.TryDecrypt(randomCipher, out var plainText);

        Assert.False(success);
        Assert.Equal(string.Empty, plainText);
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip_WorksOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var original = "merchant-password-123";
        var encrypted = MofPasswordProtector.Encrypt(original);

        var success = MofPasswordProtector.TryDecrypt(encrypted, out var decrypted);

        Assert.True(success);
        Assert.Equal(original, decrypted);
    }
}
