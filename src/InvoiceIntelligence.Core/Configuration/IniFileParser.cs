using System.IO;

namespace InvoiceIntelligence.Core.Configuration;

internal sealed class IniFileData
{
    private readonly Dictionary<string, Dictionary<string, string>> _sections;

    public IniFileData(Dictionary<string, Dictionary<string, string>> sections)
    {
        _sections = sections;
    }

    public string GetRequired(string sectionName, string key)
    {
        if (!_sections.TryGetValue(sectionName, out var section))
        {
            throw new InvalidDataException($"缺少 INI 區段 [{sectionName}]。");
        }

        if (!section.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"在區段 [{sectionName}] 缺少 INI 鍵值 '{key}'。");
        }

        return value;
    }

    public string? GetOptional(string sectionName, string key)
    {
        if (!_sections.TryGetValue(sectionName, out var section))
        {
            return null;
        }

        if (!section.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value;
    }
}

internal static class IniFileParser
{
    public static IniFileData Parse(IEnumerable<string> lines)
    {
        var sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string>? currentSection = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                var sectionName = line[1..^1].Trim();
                if (sectionName.Length == 0)
                {
                    throw new InvalidDataException("INI 區段名稱不可為空白。");
                }

                if (!sections.TryGetValue(sectionName, out currentSection))
                {
                    currentSection = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    sections[sectionName] = currentSection;
                }

                continue;
            }

            if (currentSection is null)
            {
                throw new InvalidDataException("INI 在任何區段標頭之前出現鍵值資料。");
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                throw new InvalidDataException($"INI 行格式無效: '{line}'。預期格式為 key=value。");
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            if (key.Length == 0)
            {
                throw new InvalidDataException($"INI 行格式無效: '{line}'。鍵值名稱不可為空白。");
            }

            currentSection[key] = value;
        }

        return new IniFileData(sections);
    }
}
