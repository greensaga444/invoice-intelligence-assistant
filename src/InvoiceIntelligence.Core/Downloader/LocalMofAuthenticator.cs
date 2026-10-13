namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Local authenticator that validates placeholders and issues a mock session token.
/// </summary>
public sealed class LocalMofAuthenticator : IMofAuthenticator
{
    public MofAuthenticationResult Authenticate(MofCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);

        if (IsPlaceholder(credential.Account))
        {
            return Failure("MOF 帳號尚未設定，請先更新 downloader.ini 的 [mof].account。");
        }

        if (IsPlaceholder(credential.ApiKey))
        {
            return Failure("MOF API 金鑰尚未設定，請先更新 downloader.ini 的 [mof].appKey。");
        }

        if (string.IsNullOrWhiteSpace(credential.Password))
        {
            return Failure("請輸入 MOF 密碼。");
        }

        if (credential.Password.Length < 8)
        {
            return Failure("MOF 密碼長度至少需要 8 個字元。");
        }

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(30);

        return new MofAuthenticationResult(
            IsSuccess: true,
            Message: "MOF 驗證成功，已建立下載工作階段。",
            SessionToken: $"mof-local-{Guid.NewGuid():N}",
            ExpiresAt: expiresAt);
    }

    private static bool IsPlaceholder(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return value.Contains("replace-with", StringComparison.OrdinalIgnoreCase);
    }

    private static MofAuthenticationResult Failure(string message)
    {
        return new MofAuthenticationResult(
            IsSuccess: false,
            Message: message,
            SessionToken: null,
            ExpiresAt: null);
    }
}
