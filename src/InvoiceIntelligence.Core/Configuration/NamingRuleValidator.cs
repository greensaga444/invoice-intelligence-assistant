using System.Text.RegularExpressions;

namespace InvoiceIntelligence.Core.Configuration;

public static class NamingRuleValidator
{
    private static readonly HashSet<string> AllowedPlaceholdersSet =
    [
        "taxId",
        "companyName",
        "dateFrom",
        "dateTo",
        "timestamp"
    ];

    public static IReadOnlySet<string> AllowedPlaceholders { get; } = AllowedPlaceholdersSet;

    public static void Validate(string namingRule)
    {
        if (string.IsNullOrWhiteSpace(namingRule))
        {
            throw new FormatException("Output naming rule cannot be empty.");
        }

        var matches = Regex.Matches(namingRule, "\\{(?<name>[A-Za-z][A-Za-z0-9]*)\\}");
        if (matches.Count == 0)
        {
            throw new FormatException("Output naming rule must include at least one placeholder.");
        }

        var unknownNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in matches)
        {
            var placeholder = match.Groups["name"].Value;
            if (!AllowedPlaceholdersSet.Contains(placeholder))
            {
                unknownNames.Add(placeholder);
            }
        }

        if (unknownNames.Count > 0)
        {
            throw new FormatException($"Unknown placeholder(s): {string.Join(", ", unknownNames)}.");
        }
    }
}
