using InvoiceLauncher;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;

namespace InvoiceIntelligence.Core.Tests.Launcher;

public class ModuleRuntimeCoordinatorTests
{
    [Fact]
    public void Constructor_NullDependencies_ThrowArgumentNullException()
    {
        var start = new Func<ProcessStartInfo, Process?>(_ => new Process());
        var show = new Func<Window, string, string, MessageBoxButton, MessageBoxImage, MessageBoxResult>((_, _, _, _, _) => MessageBoxResult.OK);
        var wait = new Func<Process, Task>(_ => Task.CompletedTask);
        var getExitCode = new Func<Process, int>(_ => 0);

        Assert.Throws<ArgumentNullException>(() => new ModuleRuntimeCoordinator(null!, _ => true, start, show, wait, getExitCode));
        Assert.Throws<ArgumentNullException>(() => new ModuleRuntimeCoordinator(_ => true, null!, start, show, wait, getExitCode));
        Assert.Throws<ArgumentNullException>(() => new ModuleRuntimeCoordinator(_ => true, _ => true, null!, show, wait, getExitCode));
        Assert.Throws<ArgumentNullException>(() => new ModuleRuntimeCoordinator(_ => true, _ => true, start, null!, wait, getExitCode));
        Assert.Throws<ArgumentNullException>(() => new ModuleRuntimeCoordinator(_ => true, _ => true, start, show, null!, getExitCode));
        Assert.Throws<ArgumentNullException>(() => new ModuleRuntimeCoordinator(_ => true, _ => true, start, show, wait, null!));
    }

    [Fact]
    public void EnsureModuleReady_NullArguments_ThrowArgumentNullException()
    {
        var coordinator = CreateCoordinator();
        var module = new ModuleRuntimeDefinition("Downloader", "app.exe", ".", "config.ini");
        var window = CreateWindowStub();

        Assert.Throws<ArgumentNullException>(() => coordinator.EnsureModuleReady(null!, module, _ => { }));
        Assert.Throws<ArgumentNullException>(() => coordinator.EnsureModuleReady(window, null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => coordinator.EnsureModuleReady(window, module, null!));
    }

    [Fact]
    public void EnsureModuleReady_WhenExecutableMissing_ReturnsFalseAndShowsWarning()
    {
        var messages = new List<(string Caption, string Message)>();
        var coordinator = CreateCoordinator(
            fileExists: path => !path.EndsWith("missing.exe", StringComparison.OrdinalIgnoreCase),
            onMessage: (caption, message) => messages.Add((caption, message)));

        var window = CreateWindowStub();
        var module = new ModuleRuntimeDefinition("Downloader", "missing.exe", ".", "config.ini");
        var logs = new List<string>();

        var ready = coordinator.EnsureModuleReady(window, module, logs.Add);

        Assert.False(ready);
        Assert.Single(logs);
        Assert.Contains("找不到", logs[0], StringComparison.Ordinal);
        Assert.Single(messages);
        Assert.Equal("模組未就緒", messages[0].Caption);
    }

    [Fact]
    public void EnsureModuleReady_WhenConfigMissing_ReturnsFalseAndShowsWarning()
    {
        var messages = new List<(string Caption, string Message)>();
        var coordinator = CreateCoordinator(
            fileExists: path => !path.EndsWith("missing.ini", StringComparison.OrdinalIgnoreCase),
            onMessage: (caption, message) => messages.Add((caption, message)));

        var window = CreateWindowStub();
        var module = new ModuleRuntimeDefinition("Downloader", "module.exe", ".", "missing.ini");
        var logs = new List<string>();

        var ready = coordinator.EnsureModuleReady(window, module, logs.Add);

        Assert.False(ready);
        Assert.Single(logs);
        Assert.Contains("設定檔", logs[0], StringComparison.Ordinal);
        Assert.Single(messages);
        Assert.Equal("設定未就緒", messages[0].Caption);
    }

    [Fact]
    public void EnsureModuleReady_WhenExecutableAndConfigExist_ReturnsTrue()
    {
        var messages = new List<(string Caption, string Message)>();
        var coordinator = CreateCoordinator(onMessage: (caption, message) => messages.Add((caption, message)));
        var window = CreateWindowStub();
        var module = new ModuleRuntimeDefinition("Downloader", "module.exe", ".", "config.ini");
        var logs = new List<string>();

        var ready = coordinator.EnsureModuleReady(window, module, logs.Add);

        Assert.True(ready);
        Assert.Empty(logs);
        Assert.Empty(messages);
    }

    [Fact]
    public async Task StartAndWaitForExitAsync_NullModule_ThrowsArgumentNullException()
    {
        var coordinator = CreateCoordinator();

        await Assert.ThrowsAsync<ArgumentNullException>(() => coordinator.StartAndWaitForExitAsync(null!));
    }

    [Fact]
    public async Task StartAndWaitForExitAsync_ProcessFactoryReturnsNull_ThrowsInvalidOperationException()
    {
        var coordinator = CreateCoordinator(startProcess: _ => null);
        var module = new ModuleRuntimeDefinition(
            DisplayName: "Downloader",
            ExecutablePath: "module.exe",
            WorkingDirectory: ".",
            ConfigPath: "unused.ini");

        await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.StartAndWaitForExitAsync(module));
    }

    [Fact]
    public async Task StartAndWaitForExitAsync_ProcessStarts_WaitsAndReturnsExitCode()
    {
        var process = new Process();
        var waitCalled = false;
        var coordinator = CreateCoordinator(
            startProcess: _ => process,
            waitForExitAsync: _ =>
            {
                waitCalled = true;
                return Task.CompletedTask;
            },
            getExitCode: _ => 7);

        var module = new ModuleRuntimeDefinition(
            DisplayName: "Downloader",
            ExecutablePath: "module.exe",
            WorkingDirectory: ".",
            ConfigPath: "unused.ini");

        var exitCode = await coordinator.StartAndWaitForExitAsync(module);

        Assert.True(waitCalled);
        Assert.Equal(7, exitCode);
    }

    [Fact]
    public void LaunchWithoutWaiting_NullArguments_ThrowArgumentNullException()
    {
        var coordinator = CreateCoordinator();
        var module = new ModuleRuntimeDefinition("Downloader", "app.exe", ".", "config.ini");
        var window = CreateWindowStub();

        Assert.Throws<ArgumentNullException>(() => coordinator.LaunchWithoutWaiting(null!, module, _ => { }));
        Assert.Throws<ArgumentNullException>(() => coordinator.LaunchWithoutWaiting(window, null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => coordinator.LaunchWithoutWaiting(window, module, null!));
    }

    [Fact]
    public void LaunchWithoutWaiting_WhenModuleNotReady_DoesNotStartProcess()
    {
        var startCount = 0;
        var coordinator = CreateCoordinator(
            fileExists: path => !path.EndsWith("missing.exe", StringComparison.OrdinalIgnoreCase),
            startProcess: _ =>
            {
                startCount++;
                return new Process();
            });

        var window = CreateWindowStub();
        var logs = new List<string>();
        var module = new ModuleRuntimeDefinition("Downloader", "missing.exe", ".", "config.ini");

        coordinator.LaunchWithoutWaiting(window, module, logs.Add);

        Assert.Equal(0, startCount);
        Assert.Single(logs);
    }

    [Fact]
    public void LaunchWithoutWaiting_WhenStartSucceeds_AppendsStartedLog()
    {
        var coordinator = CreateCoordinator(startProcess: _ => new Process());
        var window = CreateWindowStub();
        var logs = new List<string>();
        var module = new ModuleRuntimeDefinition("Downloader", "module.exe", ".", "config.ini");

        coordinator.LaunchWithoutWaiting(window, module, logs.Add);

        Assert.Single(logs);
        Assert.Contains("已啟動", logs[0], StringComparison.Ordinal);
    }

    [Fact]
    public void LaunchWithoutWaiting_WhenStartThrows_LogsAndShowsError()
    {
        var messages = new List<(string Caption, string Message)>();
        var coordinator = CreateCoordinator(
            startProcess: _ => throw new InvalidOperationException("start failed"),
            onMessage: (caption, message) => messages.Add((caption, message)));

        var window = CreateWindowStub();
        var logs = new List<string>();
        var module = new ModuleRuntimeDefinition("Downloader", "module.exe", ".", "config.ini");

        coordinator.LaunchWithoutWaiting(window, module, logs.Add);

        Assert.Single(logs);
        Assert.Contains("失敗", logs[0], StringComparison.Ordinal);
        Assert.Single(messages);
        Assert.Equal("啟動失敗", messages[0].Caption);
    }

    [Fact]
    public void OpenFolder_InvalidArguments_Throw()
    {
        var coordinator = CreateCoordinator();
        var window = CreateWindowStub();

        Assert.Throws<ArgumentNullException>(() => coordinator.OpenFolder(null!, "folder", _ => { }));
        Assert.Throws<ArgumentException>(() => coordinator.OpenFolder(window, " ", _ => { }));
        Assert.Throws<ArgumentNullException>(() => coordinator.OpenFolder(window, "folder", null!));
    }

    [Fact]
    public void OpenFolder_WhenFolderMissing_LogsAndShowsWarning()
    {
        var messages = new List<(string Caption, string Message)>();
        var coordinator = CreateCoordinator(
            directoryExists: _ => false,
            onMessage: (caption, message) => messages.Add((caption, message)));

        var window = CreateWindowStub();
        var logs = new List<string>();

        coordinator.OpenFolder(window, "missing-folder", logs.Add);

        Assert.Single(logs);
        Assert.Contains("資料夾不存在", logs[0], StringComparison.Ordinal);
        Assert.Single(messages);
        Assert.Equal("無法開啟", messages[0].Caption);
    }

    [Fact]
    public void OpenFolder_WhenFolderExists_StartsProcessAndLogs()
    {
        ProcessStartInfo? capturedProcessStartInfo = null;
        var coordinator = CreateCoordinator(
            directoryExists: _ => true,
            startProcess: info =>
            {
                capturedProcessStartInfo = info;
                return new Process();
            });

        var window = CreateWindowStub();
        var logs = new List<string>();

        coordinator.OpenFolder(window, "existing-folder", logs.Add);

        Assert.NotNull(capturedProcessStartInfo);
        Assert.Equal("existing-folder", capturedProcessStartInfo!.FileName);
        Assert.Single(logs);
        Assert.Contains("已開啟資料夾", logs[0], StringComparison.Ordinal);
    }

    private static ModuleRuntimeCoordinator CreateCoordinator(
        Func<string, bool>? fileExists = null,
        Func<string, bool>? directoryExists = null,
        Func<ProcessStartInfo, Process?>? startProcess = null,
        Action<string, string>? onMessage = null,
        Func<Process, Task>? waitForExitAsync = null,
        Func<Process, int>? getExitCode = null)
    {
        return new ModuleRuntimeCoordinator(
            fileExists ?? (_ => true),
            directoryExists ?? (_ => true),
            startProcess ?? (_ => new Process()),
            (owner, message, caption, button, image) =>
            {
                onMessage?.Invoke(caption, message);
                return MessageBoxResult.OK;
            },
            waitForExitAsync ?? (_ => Task.CompletedTask),
            getExitCode ?? (_ => 0));
    }

    private static Window CreateWindowStub()
    {
        return (Window)RuntimeHelpers.GetUninitializedObject(typeof(Window));
    }
}
