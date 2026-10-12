using System.Globalization;

namespace InvoiceIntelligence.Core.Processor;

public sealed class InMemoryCaseCache : ICaseCache
{
    private readonly Dictionary<string, InvoiceDecision> _cases;

    public InMemoryCaseCache(IEnumerable<CachedInvoiceDecisionCase> cases)
    {
        ArgumentNullException.ThrowIfNull(cases);

        _cases = new Dictionary<string, InvoiceDecision>(StringComparer.OrdinalIgnoreCase);

        foreach (var caseItem in cases)
        {
            var key = BuildKey(caseItem.VendorTaxId, caseItem.ItemName, caseItem.NetAmount);
            _cases[key] = caseItem.Decision;
        }
    }

    public InvoiceDecision? TryResolve(InvoiceAnalysisRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var key = BuildKey(record.VendorTaxId, record.ItemName, record.NetAmount);
        return _cases.TryGetValue(key, out var decision) ? decision : null;
    }

    private static string BuildKey(string vendorTaxId, string itemName, decimal netAmount)
    {
        var normalizedVendorTaxId = (vendorTaxId ?? string.Empty).Trim();
        var normalizedItemName = (itemName ?? string.Empty).Trim().ToUpperInvariant();
        var normalizedAmount = decimal.Round(netAmount, 2, MidpointRounding.AwayFromZero)
            .ToString("0.00", CultureInfo.InvariantCulture);

        return $"{normalizedVendorTaxId}|{normalizedItemName}|{normalizedAmount}";
    }
}
