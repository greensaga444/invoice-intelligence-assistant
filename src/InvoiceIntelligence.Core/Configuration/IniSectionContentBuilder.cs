using System.Text;

namespace InvoiceIntelligence.Core.Configuration;

internal sealed class IniSectionContentBuilder
{
    private readonly StringBuilder _builder = new();
    private readonly string _lineBreak = Environment.NewLine;

    public IniSectionContentBuilder BeginSection(string sectionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);
        _builder.Append('[').Append(sectionName).Append(']').Append(_lineBreak);
        return this;
    }

    public IniSectionContentBuilder Add(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _builder.Append(key).Append(" = ").Append(value ?? string.Empty).Append(_lineBreak);
        return this;
    }

    public IniSectionContentBuilder AddBlankLine()
    {
        _builder.Append(_lineBreak);
        return this;
    }

    public override string ToString()
    {
        return _builder.ToString();
    }
}
