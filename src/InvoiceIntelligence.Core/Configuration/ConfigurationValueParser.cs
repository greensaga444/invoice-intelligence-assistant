using System.Globalization;

namespace InvoiceIntelligence.Core.Configuration;

public static class ConfigurationValueParser
{
    public static bool TryParseLowConfidenceThreshold(string? value, out decimal threshold)
    {
        threshold = 0m;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();
        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out threshold)
            && !decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.CurrentCulture, out threshold))
        {
            return false;
        }

        if (threshold is < 0m or > 1m)
        {
            return false;
        }

        threshold = decimal.Round(threshold, 2, MidpointRounding.AwayFromZero);
        return true;
    }
}
