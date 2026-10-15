using System.Diagnostics;
using System.IO;
using System.Windows;

namespace InvoiceLauncher;

internal sealed class ModuleRuntimeCoordinator
{
    public bool EnsureModuleReady(Window owner, ModuleRuntimeDefinition module, Action<string> appendLog)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(module);
        ArgumentNullException.ThrowIfNull(appendLog);

        if (!File.Exists(module.ExecutablePath))
        {
            var executableMessage = $"找不到 {module.DisplayName} 可執行檔：{module.ExecutablePath}";
            appendLog(executableMessage);
            MessageBox.Show(owner, executableMessage, "模組未就緒", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (File.Exists(module.ConfigPath))
        {
            return true;
        }

        var configMessage = $"找不到 {module.DisplayName} 設定檔：{module.ConfigPath}";
        appendLog(configMessage);
        MessageBox.Show(owner, configMessage, "設定未就緒", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    public async Task<int> StartAndWaitForExitAsync(ModuleRuntimeDefinition module)
    {
        ArgumentNullException.ThrowIfNull(module);

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = module.ExecutablePath,
            WorkingDirectory = module.WorkingDirectory,
            UseShellExecute = true
        });

        if (process is null)
        {
            throw new InvalidOperationException($"無法啟動程序：{module.ExecutablePath}");
        }

        await process.WaitForExitAsync();
        return process.ExitCode;
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
            Process.Start(new ProcessStartInfo
            {
                FileName = module.ExecutablePath,
                WorkingDirectory = module.WorkingDirectory,
                UseShellExecute = true
            });

            appendLog($"{module.DisplayName} 已啟動。");
        }
        catch (Exception ex)
        {
            appendLog($"啟動 {module.DisplayName} 失敗：{ex.Message}");
            MessageBox.Show(owner, ex.Message, "啟動失敗", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void OpenFolder(Window owner, string folderPath, Action<string> appendLog)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
        ArgumentNullException.ThrowIfNull(appendLog);

        if (!Directory.Exists(folderPath))
        {
            appendLog($"資料夾不存在：{folderPath}");
            MessageBox.Show(owner, $"資料夾不存在：{folderPath}", "無法開啟", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo
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
    string ConfigPath);
