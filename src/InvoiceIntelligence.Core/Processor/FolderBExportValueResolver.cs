using System.Globalization;

namespace InvoiceIntelligence.Core.Processor;

internal static class FolderBExportValueResolver
{
    public static string ResolveTaxType(InvoiceAnalysisRecord record)
    {
        return record.TaxAmount > 0m ? "5" : "*";
    }

    public static string ResolveDeductibleFlag(string deductibleStatus)
    {
        if (deductibleStatus.Contains("不可", StringComparison.Ordinal))
        {
            return "N";
        }

        if (deductibleStatus.Contains("可抵", StringComparison.Ordinal))
        {
            return "Y";
        }

        return "N";
    }

    public static string ResolveVoucherCategory(string accountingCategory)
    {
        if (accountingCategory.Contains("固定", StringComparison.Ordinal)
            || accountingCategory.Contains("設備", StringComparison.Ordinal)
            || accountingCategory.Contains("資產", StringComparison.Ordinal))
        {
            return "3";
        }

        if (accountingCategory.Contains("費", StringComparison.Ordinal))
        {
            return "2";
        }

        return "1";
    }

    public static string ToRocYear(DateOnly date)
    {
        return (date.Year - 1911).ToString("000", CultureInfo.InvariantCulture);
    }

    public static string ToRocYearMonth(DateOnly date)
    {
        return $"{ToRocYear(date)}{date.Month:00}";
    }

    public static string ToRocDate(DateOnly date)
    {
        return $"{ToRocYear(date)}{date.Month:00}{date.Day:00}";
    }
}
