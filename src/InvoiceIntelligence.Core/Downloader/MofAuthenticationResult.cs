namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Authentication outcome returned by <see cref="IMofAuthenticator"/>.
/// </summary>
public sealed record MofAuthenticationResult(
    bool IsSuccess,
    string Message,
    string? SessionToken,
    DateTimeOffset? ExpiresAt);
