namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Credential payload used for MOF authentication.
/// </summary>
public sealed record MofCredential(string Account, string ApiKey, string Password);
