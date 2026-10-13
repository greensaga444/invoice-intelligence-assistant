namespace InvoiceIntelligence.Core.Downloader;

/// <summary>
/// Domain exception for official API failures with HTTP status and retry hint.
/// </summary>
public sealed class MofOfficialInvoiceApiException : Exception
{
    public MofOfficialInvoiceApiException(string message, int? statusCode, bool isTransient, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        IsTransient = isTransient;
    }

    /// <summary>
    /// HTTP status code when available; null usually means network-level failure.
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// Indicates whether caller may retry this failure safely.
    /// </summary>
    public bool IsTransient { get; }
}