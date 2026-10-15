namespace InvoiceIntelligence.Core.Configuration;

public static class MofApiCredentialResolver
{
    public static string ResolveAppId(string? appId, string? appKey)
    {
        return ResolvePrimaryOrFallback(appId, appKey);
    }

    public static string ResolveAppKey(string? appId, string? appKey)
    {
        return ResolvePrimaryOrFallback(appKey, appId);
    }

    public static string ResolvePrimaryOrFallback(string? primary, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(primary))
        {
            return primary.Trim();
        }

        return fallback?.Trim() ?? string.Empty;
    }
}
