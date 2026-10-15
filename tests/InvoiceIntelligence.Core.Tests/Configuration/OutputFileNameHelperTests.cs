using InvoiceIntelligence.Core.Configuration;

namespace InvoiceIntelligence.Core.Tests.Configuration;

public class OutputFileNameHelperTests
{
    [Fact]
    public void BuildFileName_ReplacesPlaceholdersAndAddsMissingExtension()
    {
        var result = OutputFileNameHelper.BuildFileName(
            namingRule: "{taxId}_{companyName}_{dateFrom}_{dateTo}_{timestamp}",
            taxId: "12345678",
            companyName: "Acme Ltd",
            dateFrom: new DateOnly(2026, 1, 2),
            dateTo: new DateOnly(2026, 1, 31),
            timestamp: new DateTimeOffset(2026, 2, 1, 11, 22, 33, TimeSpan.Zero),
            extension: "csv");

        Assert.Equal("12345678_Acme Ltd_20260102_20260131_20260201112233.csv", result);
    }

    [Fact]
    public void BuildFileName_DoesNotDuplicateExtensionWhenAlreadyPresent()
    {
        var result = OutputFileNameHelper.BuildFileName(
            namingRule: "{taxId}.XLSX",
            taxId: "A100",
            companyName: "Contoso",
            dateFrom: new DateOnly(2026, 1, 1),
            dateTo: new DateOnly(2026, 1, 1),
            timestamp: DateTimeOffset.UtcNow,
            extension: ".xlsx");

        Assert.Equal("A100.XLSX", result);
    }

    [Fact]
    public void BuildFileName_ReplacesInvalidCharactersInSegments()
    {
        var invalidCharacter = Path.GetInvalidFileNameChars().FirstOrDefault(character => character != '\0');
        if (invalidCharacter == default)
        {
            invalidCharacter = '/';
        }

        var result = OutputFileNameHelper.BuildFileName(
            namingRule: "{companyName}",
            taxId: "A100",
            companyName: $"Alpha{invalidCharacter}Beta",
            dateFrom: new DateOnly(2026, 1, 1),
            dateTo: new DateOnly(2026, 1, 1),
            timestamp: DateTimeOffset.UtcNow,
            extension: "txt");

        Assert.Equal("Alpha_Beta.txt", result);
    }

    [Fact]
    public void BuildFileName_WithWhitespaceExtension_ThrowsArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() => OutputFileNameHelper.BuildFileName(
            namingRule: "{taxId}",
            taxId: "A100",
            companyName: "Contoso",
            dateFrom: new DateOnly(2026, 1, 1),
            dateTo: new DateOnly(2026, 1, 1),
            timestamp: DateTimeOffset.UtcNow,
            extension: " "));
    }

    [Fact]
    public void BuildFileName_WithWhitespaceSegments_UsesDefaultPlaceholderText()
    {
        var result = OutputFileNameHelper.BuildFileName(
            namingRule: "{taxId}_{companyName}",
            taxId: " ",
            companyName: " ",
            dateFrom: new DateOnly(2026, 1, 1),
            dateTo: new DateOnly(2026, 1, 1),
            timestamp: DateTimeOffset.UtcNow,
            extension: "txt");

        Assert.Equal("未提供_未提供.txt", result);
    }

    [Fact]
    public void EnsureUniquePath_WhenFileDoesNotExist_ReturnsSamePath()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"invoice-intelligence-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var path = Path.Combine(tempDirectory, "result.csv");

            var uniquePath = OutputFileNameHelper.EnsureUniquePath(path);

            Assert.Equal(path, uniquePath);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void EnsureUniquePath_WhenBaseAndSecondCandidateExist_ReturnsThirdCandidate()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"invoice-intelligence-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var basePath = Path.Combine(tempDirectory, "result.csv");
            var secondCandidatePath = Path.Combine(tempDirectory, "result-2.csv");

            File.WriteAllText(basePath, "existing");
            File.WriteAllText(secondCandidatePath, "existing");

            var uniquePath = OutputFileNameHelper.EnsureUniquePath(basePath);

            Assert.Equal(Path.Combine(tempDirectory, "result-3.csv"), uniquePath);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void EnsureUniquePath_WhenRelativePathHasNoDirectory_ReturnsIndexedRelativePath()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"invoice-intelligence-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        var originalCurrentDirectory = Environment.CurrentDirectory;

        try
        {
            Environment.CurrentDirectory = tempDirectory;
            File.WriteAllText("result.csv", "existing");

            var uniquePath = OutputFileNameHelper.EnsureUniquePath("result.csv");

            Assert.Equal("result-2.csv", uniquePath);
        }
        finally
        {
            Environment.CurrentDirectory = originalCurrentDirectory;
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}