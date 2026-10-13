using System.Globalization;
using System.Text;

namespace InvoiceIntelligence.Core.Configuration;

public static class OutputFileNameHelper
{
    private static readonly HashSet<char> InvalidFileNameCharacters = new(Path.GetInvalidFileNameChars());

    public static string BuildFileName(
        string namingRule,
        string taxId,
        string companyName,
        DateOnly dateFrom,
        DateOnly dateTo,
        DateTimeOffset timestamp,
        string extension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(namingRule);
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);

        var normalizedExtension = extension.StartsWith(".", StringComparison.Ordinal)
            ? extension
            : $".{extension}";

        var fileName = namingRule
            .Replace("{taxId}", SanitizeSegment(taxId), StringComparison.OrdinalIgnoreCase)
            .Replace("{companyName}", SanitizeSegment(companyName), StringComparison.OrdinalIgnoreCase)
            .Replace("{dateFrom}", dateFrom.ToString("yyyyMMdd", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{dateTo}", dateTo.ToString("yyyyMMdd", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{timestamp}", timestamp.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);

        if (!fileName.EndsWith(normalizedExtension, StringComparison.OrdinalIgnoreCase))
        {
            fileName += normalizedExtension;
        }

        return fileName;
    }

    public static string EnsureUniquePath(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            return filePath;
        }

        var directory = Path.GetDirectoryName(filePath)
            ?? throw new InvalidOperationException("輸出路徑缺少目錄資訊。");
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);

        for (var index = 2; index <= 9999; index++)
        {
            var candidatePath = Path.Combine(directory, $"{fileNameWithoutExtension}-{index}{extension}");
            if (!File.Exists(candidatePath))
            {
                return candidatePath;
            }
        }

        throw new IOException("無法產生唯一輸出檔名，請清理輸出資料夾後重試。");
    }

    private static string SanitizeSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "未提供";
        }

        var builder = new StringBuilder(value.Length);

        foreach (var character in value.Trim())
        {
            if (InvalidFileNameCharacters.Contains(character))
            {
                builder.Append('_');
                continue;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }
}