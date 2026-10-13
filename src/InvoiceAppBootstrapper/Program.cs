using System.Diagnostics;

internal static class Program
{
    private static int Main(string[] args)
    {
        var currentExePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(currentExePath))
        {
            currentExePath = Process.GetCurrentProcess().MainModule?.FileName;
        }

        if (string.IsNullOrWhiteSpace(currentExePath))
        {
            Console.Error.WriteLine("Unable to determine current executable path.");
            return 1;
        }

        var executableName = Path.GetFileName(currentExePath);
        var appBaseDirectory = AppContext.BaseDirectory;
        var runtimeDirectory = Path.Combine(appBaseDirectory, "libs");
        var runtimeExecutablePath = Path.Combine(runtimeDirectory, executableName);

        if (!File.Exists(runtimeExecutablePath))
        {
            Console.Error.WriteLine($"Runtime executable not found: {runtimeExecutablePath}");
            return 1;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = runtimeExecutablePath,
            WorkingDirectory = runtimeDirectory,
            UseShellExecute = false
        };

        foreach (var argument in args)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            Console.Error.WriteLine($"Unable to start runtime executable: {runtimeExecutablePath}");
            return 1;
        }

        process.WaitForExit();
        return process.ExitCode;
    }
}
