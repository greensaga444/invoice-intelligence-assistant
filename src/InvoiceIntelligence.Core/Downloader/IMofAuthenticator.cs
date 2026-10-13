namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Authenticates MOF credentials and returns a session token result.
/// </summary>
public interface IMofAuthenticator
{
    MofAuthenticationResult Authenticate(MofCredential credential);
}
