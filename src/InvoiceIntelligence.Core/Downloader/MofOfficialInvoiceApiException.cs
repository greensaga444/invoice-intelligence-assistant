namespace InvoiceIntelligence.Core.Downloader;

public sealed class MofOfficialInvoiceApiException : Exception
{
    public MofOfficialInvoiceApiException(string message, int? statusCode, bool isTransient, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        IsTransient = isTransient;
    }

    public int? StatusCode { get; }

    public bool IsTransient { get; }
}