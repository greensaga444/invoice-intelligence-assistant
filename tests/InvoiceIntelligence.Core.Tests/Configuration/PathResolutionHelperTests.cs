using InvoiceIntelligence.Core.Configuration;

namespace InvoiceIntelligence.Core.Tests.Configuration;

public class PathResolutionHelperTests
{
    [Fact]
    public void ResolveConfigPath_WhenConfigExistsInParent_ReturnsExistingPath()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"path-resolution-{Guid.NewGuid():N}");
        var nestedPath = Path.Combine(rootPath, "a", "b", "c");
        var configDirectory = Path.Combine(rootPath, "config");
        Directory.CreateDirectory(nestedPath);
        Directory.CreateDirectory(configDirectory);

        var expectedPath = Path.Combine(configDirectory, "downloader.ini");
        File.WriteAllText(expectedPath, "key=value");

        try
        {
            var resolvedPath = PathResolutionHelper.ResolveConfigPath("downloader.ini", nestedPath);

            Assert.Equal(expectedPath, resolvedPath);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void ResolveConfigPath_WhenConfigDoesNotExist_ReturnsFallbackPath()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"path-resolution-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootPath);

        try
        {
            var resolvedPath = PathResolutionHelper.ResolveConfigPath("missing.ini", rootPath);

            Assert.Equal(Path.Combine(rootPath, "config", "missing.ini"), resolvedPath);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void ResolveConfigPath_WithWhitespaceFileName_ThrowsArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() => PathResolutionHelper.ResolveConfigPath(" "));
    }

    [Fact]
    public void ResolveSolutionRootPath_ReturnsParentOfConfigDirectory()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"path-resolution-{Guid.NewGuid():N}");
        var configDirectory = Path.Combine(rootPath, "config");
        Directory.CreateDirectory(configDirectory);
        var configPath = Path.Combine(configDirectory, "processor.ini");
        File.WriteAllText(configPath, "key=value");

        try
        {
            var solutionRoot = PathResolutionHelper.ResolveSolutionRootPath(configPath);

            Assert.Equal(rootPath, solutionRoot);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void ResolveConfiguredPath_WhenPathIsRooted_ReturnsOriginal()
    {
        var rootedPath = Path.Combine(Path.GetTempPath(), "absolute", "value.txt");

        var resolvedPath = PathResolutionHelper.ResolveConfiguredPath("C:\\unused-root", rootedPath);

        Assert.Equal(rootedPath, resolvedPath);
    }

    [Fact]
    public void ResolveConfiguredPath_WhenPathIsRelative_ReturnsFullPath()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"path-resolution-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootPath);

        try
        {
            var resolvedPath = PathResolutionHelper.ResolveConfiguredPath(rootPath, ".\\data\\file.txt");
            var expectedPath = Path.GetFullPath(Path.Combine(rootPath, ".\\data\\file.txt"));

            Assert.Equal(expectedPath, resolvedPath);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void ResolveDialogInitialPath_WhenDirectoryExists_ReturnsDirectoryOnly()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), $"path-resolution-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directoryPath);

        try
        {
            var result = PathResolutionHelper.ResolveDialogInitialPath(directoryPath);

            Assert.Equal(directoryPath, result.InitialDirectory);
            Assert.Null(result.FileName);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public void ResolveDialogInitialPath_WhenParentDirectoryExists_ReturnsParentAndFileName()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), $"path-resolution-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directoryPath);
        var configuredPath = Path.Combine(directoryPath, "sample.xlsx");

        try
        {
            var result = PathResolutionHelper.ResolveDialogInitialPath(configuredPath);

            Assert.Equal(directoryPath, result.InitialDirectory);
            Assert.Equal("sample.xlsx", result.FileName);
        }
        finally
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    [Fact]
    public void ResolveDialogInitialPath_WhenPathIsInvalid_ReturnsNullTuple()
    {
        var configuredPath = Path.Combine(Path.GetTempPath(), $"path-resolution-missing-{Guid.NewGuid():N}", "missing.xlsx");

        var result = PathResolutionHelper.ResolveDialogInitialPath(configuredPath);

        Assert.Null(result.InitialDirectory);
        Assert.Null(result.FileName);
    }
}
