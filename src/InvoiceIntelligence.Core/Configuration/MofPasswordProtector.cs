using System.Security.Cryptography;
using System.Text;

namespace InvoiceIntelligence.Core.Configuration;

public static class MofPasswordProtector
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("InvoiceIntelligenceAssistant.MofPassword.v1");

    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return string.Empty;
        }

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("MOF 密碼加密僅支援 Windows 平台。");
        }

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);

        return Convert.ToBase64String(cipherBytes);
    }

    public static bool TryDecrypt(string? encryptedText, out string plainText)
    {
        plainText = string.Empty;

        if (string.IsNullOrWhiteSpace(encryptedText))
        {
            return false;
        }

        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            var cipherBytes = Convert.FromBase64String(encryptedText);
            var plainBytes = ProtectedData.Unprotect(cipherBytes, Entropy, DataProtectionScope.CurrentUser);
            plainText = Encoding.UTF8.GetString(plainBytes);
            return plainText.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}