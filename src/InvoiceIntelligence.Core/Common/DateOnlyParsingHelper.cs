using System.Globalization;

namespace InvoiceIntelligence.Core.Common;

public static class DateOnlyParsingHelper
{
    public static bool TryParseDate(string input, out DateOnly value)
    {
        if (DateOnly.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
        {
            return true;
        }

        return DateOnly.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.None, out value);
    }
}
