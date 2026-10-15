namespace InvoiceIntelligence.Core.Processor;

public enum FolderAFileCategory
{
    Unknown = 0,
    Invoice = 1,
    Allowance = 2
}

public static class FolderAFileCategoryHelper
{
    private const string LegacyAllowanceTypoSuffix = "-allwance";

    public static FolderAFileCategory GetCategory(
        string filePathOrName,
        string invoiceFileSuffix = "invoice",
        string allowanceFileSuffix = "allowance")
    {
        if (string.IsNullOrWhiteSpace(filePathOrName))
        {
            return FolderAFileCategory.Unknown;
        }

        var invoiceSuffix = NormalizeSuffix(invoiceFileSuffix, "invoice");
        var allowanceSuffix = NormalizeSuffix(allowanceFileSuffix, "allowance");
        var fileName = Path.GetFileNameWithoutExtension(Path.GetFileName(filePathOrName));
        if (fileName.EndsWith(invoiceSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return FolderAFileCategory.Invoice;
        }

        if (fileName.EndsWith(allowanceSuffix, StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(LegacyAllowanceTypoSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return FolderAFileCategory.Allowance;
        }

        return FolderAFileCategory.Unknown;
    }

    public static bool IsPreferredSourceFile(
        string filePathOrName,
        string invoiceFileSuffix = "invoice",
        string allowanceFileSuffix = "allowance")
    {
        return GetCategory(filePathOrName, invoiceFileSuffix, allowanceFileSuffix) != FolderAFileCategory.Unknown;
    }

    public static string BuildGroupKey(
        string sourceFileName,
        string invoiceFileSuffix = "invoice",
        string allowanceFileSuffix = "allowance")
    {
        if (string.IsNullOrWhiteSpace(sourceFileName))
        {
            return string.Empty;
        }

        var invoiceSuffix = NormalizeSuffix(invoiceFileSuffix, "invoice");
        var allowanceSuffix = NormalizeSuffix(allowanceFileSuffix, "allowance");
        var normalizedFileName = Path.GetFileName(sourceFileName);
        var baseName = Path.GetFileNameWithoutExtension(normalizedFileName);

        if (baseName.EndsWith(invoiceSuffix, StringComparison.OrdinalIgnoreCase))
        {
            var trimmed = baseName[..^invoiceSuffix.Length];
            return string.IsNullOrWhiteSpace(trimmed)
                ? normalizedFileName
                : trimmed;
        }

        if (baseName.EndsWith(allowanceSuffix, StringComparison.OrdinalIgnoreCase))
        {
            var trimmed = baseName[..^allowanceSuffix.Length];
            return string.IsNullOrWhiteSpace(trimmed)
                ? normalizedFileName
                : trimmed;
        }

        if (baseName.EndsWith(LegacyAllowanceTypoSuffix, StringComparison.OrdinalIgnoreCase))
        {
            var trimmed = baseName[..^LegacyAllowanceTypoSuffix.Length];
            return string.IsNullOrWhiteSpace(trimmed)
                ? normalizedFileName
                : trimmed;
        }

        return baseName;
    }

    private static string NormalizeSuffix(string configuredSuffix, string fallbackSuffix)
    {
        var suffix = string.IsNullOrWhiteSpace(configuredSuffix)
            ? fallbackSuffix
            : configuredSuffix.Trim();

        suffix = suffix.TrimStart('-');
        if (suffix.Length == 0)
        {
            suffix = fallbackSuffix;
        }

        return $"-{suffix}";
    }
}