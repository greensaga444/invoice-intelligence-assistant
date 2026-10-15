using System.IO;
using InvoiceIntelligence.Core.Configuration;

namespace InvoiceIntelligence.Core.Tests.Configuration;

public class IniFileParserTests
{
    [Fact]
    public void Parse_ValidContent_SectionAndKeysAreCaseInsensitive()
    {
        var lines = new[]
        {
            "; comment",
            "# comment",
            "[module]",
            "name = Downloader",
            "[MODULE]",
            "name = Processor",
            "[mof]",
            "appKey = sample-key",
            "passwordEncrypted = "
        };

        var data = IniFileParser.Parse(lines);

        Assert.Equal("Processor", data.GetRequired("module", "name"));
        Assert.Equal("sample-key", data.GetRequired("MOF", "APPKEY"));
        Assert.Null(data.GetOptional("mof", "passwordEncrypted"));
    }

    [Fact]
    public void Parse_KeyValueBeforeSection_ThrowsInvalidDataException()
    {
        var ex = Assert.Throws<InvalidDataException>(() => IniFileParser.Parse(["name=value"]));

        Assert.Contains("區段標頭之前", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_InvalidKeyValueLine_ThrowsInvalidDataException()
    {
        var ex = Assert.Throws<InvalidDataException>(() => IniFileParser.Parse([
            "[module]",
            "name"
        ]));

        Assert.Contains("key=value", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_EmptySectionName_ThrowsInvalidDataException()
    {
        var ex = Assert.Throws<InvalidDataException>(() => IniFileParser.Parse(["[]"]));

        Assert.Contains("區段名稱", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_EmptyKey_ThrowsInvalidDataException()
    {
        var ex = Assert.Throws<InvalidDataException>(() => IniFileParser.Parse([
            "[module]",
            "= value"
        ]));

        Assert.Contains("key=value", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetRequired_MissingSectionOrKey_ThrowsInvalidDataException()
    {
        var data = IniFileParser.Parse([
            "[module]",
            "name = Processor"
        ]);

        Assert.Throws<InvalidDataException>(() => data.GetRequired("mof", "appKey"));
        Assert.Throws<InvalidDataException>(() => data.GetRequired("module", "missing"));
    }

    [Fact]
    public void GetRequired_BlankValue_ThrowsInvalidDataException()
    {
        var data = IniFileParser.Parse([
            "[module]",
            "name = "
        ]);

        Assert.Throws<InvalidDataException>(() => data.GetRequired("module", "name"));
        Assert.Null(data.GetOptional("module", "name"));
    }
}
