using System.IO;

namespace InvoiceIntelligence.Core.Configuration;

public static class PathResolutionHelper
{
    public static string ResolveConfigPath(string fileName, string? baseDirectory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var current = new DirectoryInfo(baseDirectory ?? AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "config", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return Path.Combine(baseDirectory ?? AppContext.BaseDirectory, "config", fileName);
    }

    public static string ResolveSolutionRootPath(string configPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configPath);

        var configDirectory = Path.GetDirectoryName(configPath)
            ?? throw new InvalidOperationException("無法解析設定檔目錄路徑。");

        var solutionRootDirectory = Directory.GetParent(configDirectory)?.FullName;
        return solutionRootDirectory ?? AppContext.BaseDirectory;
    }

    public static string ResolveConfiguredPath(string rootPath, string configuredPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(configuredPath);

        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        return Path.GetFullPath(Path.Combine(rootPath, configuredPath));
    }

    public static (string? InitialDirectory, string? FileName) ResolveDialogInitialPath(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return (null, null);
        }

        if (Directory.Exists(configuredPath))
        {
            return (configuredPath, null);
        }

        var directoryPath = Path.GetDirectoryName(configuredPath);
        if (!string.IsNullOrWhiteSpace(directoryPath) && Directory.Exists(directoryPath))
        {
            return (directoryPath, Path.GetFileName(configuredPath));
        }

        return (null, null);
    }
}
