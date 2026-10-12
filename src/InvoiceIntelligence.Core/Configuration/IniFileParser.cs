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
            throw new InvalidDataException($"Missing INI section [{sectionName}].");
        }

        if (!section.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"Missing INI key '{key}' under section [{sectionName}].");
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
                    throw new InvalidDataException("INI section name cannot be empty.");
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
                throw new InvalidDataException("INI contains key-value data before any section header.");
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                throw new InvalidDataException($"Invalid INI line: '{line}'. Expected key=value format.");
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            if (key.Length == 0)
            {
                throw new InvalidDataException($"Invalid INI line: '{line}'. Key cannot be empty.");
            }

            currentSection[key] = value;
        }

        return new IniFileData(sections);
    }
}
