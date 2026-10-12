namespace InvoiceIntelligence.Core.Downloader;

public interface IMofAuthenticator
{
    MofAuthenticationResult Authenticate(MofCredential credential);
}
