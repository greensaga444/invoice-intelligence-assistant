namespace InvoiceIntelligence.Core.Downloader;

public sealed record MofAuthenticationResult(
    bool IsSuccess,
    string Message,
    string? SessionToken,
    DateTimeOffset? ExpiresAt);
