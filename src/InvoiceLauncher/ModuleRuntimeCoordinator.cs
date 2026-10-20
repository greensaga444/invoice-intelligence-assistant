using System.Diagnostics;
using System.IO;
using System.Windows;

namespace InvoiceLauncher;

internal sealed class ModuleRuntimeCoordinator
{
    private readonly Func<string, bool> _fileExists;
    private readonly Func<string, bool> _directoryExists;
    private readonly Func<ProcessStartInfo, Process?> _startProcess;
    private readonly Func<Window, string, string, MessageBoxButton, MessageBoxImage, MessageBoxResult> _showMessage;
    private readonly Func<Process, Task> _waitForExitAsync;
    private readonly Func<Process, int> _getExitCode;

    public ModuleRuntimeCoordinator()
        : this(
            File.Exists,
            Directory.Exists,
            Process.Start,
            (owner, message, caption, button, image) => MessageBox.Show(owner, message, caption, button, image),
            process => process.WaitForExitAsync(),
            process => process.ExitCode)
    {
    }

    internal ModuleRuntimeCoordinator(
        Func<string, bool> fileExists,
        Func<string, bool> directoryExists,
        Func<ProcessStartInfo, Process?> startProcess,
        Func<Window, string, string, MessageBoxButton, MessageBoxImage, MessageBoxResult> showMessage,
        Func<Process, Task> waitForExitAsync,
        Func<Process, int> getExitCode)
    {
        _fileExists = fileExists ?? throw new ArgumentNullException(nameof(fileExists));
        _directoryExists = directoryExists ?? throw new ArgumentNullException(nameof(directoryExists));
        _startProcess = startProcess ?? throw new ArgumentNullException(nameof(startProcess));
        _showMessage = showMessage ?? throw new ArgumentNullException(nameof(showMessage));
        _waitForExitAsync = waitForExitAsync ?? throw new ArgumentNullException(nameof(waitForExitAsync));
        _getExitCode = getExitCode ?? throw new ArgumentNullException(nameof(getExitCode));
    }

    public bool EnsureModuleReady(Window owner, ModuleRuntimeDefinition module, Action<string> appendLog)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(appendLog);

        if (!_fileExists(module.ExecutablePath))
        {
            var executableMessage = $"找不到 {module.DisplayName} 可執行檔：{module.ExecutablePath}";
            appendLog(executableMessage);
            _showMessage(owner, executableMessage, "模組未就緒", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!module.RequiresConfig)
        {
            return true;
        }

        if (_fileExists(module.ConfigPath))
        {
            return true;
        }

        var configMessage = $"找不到 {module.DisplayName} 設定檔：{module.ConfigPath}";
        appendLog(configMessage);
        _showMessage(owner, configMessage, "設定未就緒", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    public async Task<int> StartAndWaitForExitAsync(ModuleRuntimeDefinition module)
    {
        ArgumentNullException.ThrowIfNull(module);

        var startInfo = new ProcessStartInfo
        {
            FileName = module.ExecutablePath,
            WorkingDirectory = module.WorkingDirectory,
            UseShellExecute = true
        };

        foreach (var argument in module.Arguments ?? Array.Empty<string>())
        {
            startInfo.ArgumentList.Add(argument);
        }

        var process = _startProcess(startInfo);

        if (process is null)
        {
            throw new InvalidOperationException($"無法啟動程序：{module.ExecutablePath}");
        }

        await _waitForExitAsync(process);
        return _getExitCode(process);
    }

    public void LaunchWithoutWaiting(Window owner, ModuleRuntimeDefinition module, Action<string> appendLog)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(appendLog);

        if (!EnsureModuleReady(owner, module, appendLog))
        {
            return;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = module.ExecutablePath,
                WorkingDirectory = module.WorkingDirectory,
                UseShellExecute = true
            };

            foreach (var argument in module.Arguments ?? Array.Empty<string>())
            {
                startInfo.ArgumentList.Add(argument);
            }

            _startProcess(startInfo);

            appendLog($"{module.DisplayName} 已啟動。");
        }
        catch (Exception ex)
        {
            appendLog($"啟動 {module.DisplayName} 失敗：{ex.Message}");
            _showMessage(owner, ex.Message, "啟動失敗", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void OpenFolder(Window owner, string folderPath, Action<string> appendLog)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
        ArgumentNullException.ThrowIfNull(appendLog);

        if (!_directoryExists(folderPath))
        {
            appendLog($"資料夾不存在：{folderPath}");
            _showMessage(owner, $"資料夾不存在：{folderPath}", "無法開啟", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _startProcess(new ProcessStartInfo
        {
            FileName = folderPath,
            UseShellExecute = true
        });

        appendLog($"已開啟資料夾：{folderPath}");
    }
}

internal sealed record ModuleRuntimeDefinition(
    string DisplayName,
    string ExecutablePath,
    string WorkingDirectory,
    string ConfigPath,
    IReadOnlyList<string>? Arguments = null,
    bool RequiresConfig = true);
