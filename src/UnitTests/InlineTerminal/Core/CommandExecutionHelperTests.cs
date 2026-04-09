using FluentAssertions;
using WindowSill.InlineTerminal.Core.Commands;
using WindowSill.InlineTerminal.Core.Shell;
using Path = System.IO.Path;

namespace UnitTests.InlineTerminal.Core;

public class CommandExecutionHelperTests
{
    /// <summary>
    /// Creates a cmd.exe-based <see cref="ShellInfo"/> for testing.
    /// </summary>
    private static ShellInfo CreateCmdShell()
    {
        string cmdPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "cmd.exe");

        return new ShellInfo(
            "Command Prompt",
            cmdPath,
            escapeCommand: command => command.Replace("\"", "\\\""),
            buildArguments: command => $"/c {command}",
            buildElevatedArguments: (command, outputFile) =>
                $"/c \"{command} > \"{outputFile}\" 2>&1\"");
    }

    [Fact]
    public async Task ExecuteAsync_EchoCommand_ReturnsZeroExitCodeAsync()
    {
        // Arrange
        ShellInfo shell = CreateCmdShell();
        List<string> output = [];

        // Act
        int exitCode = await CommandExecutionHelper.ExecuteAsync(
            "echo hello",
            shell,
            Directory.GetCurrentDirectory(),
            output.Add,
            CancellationToken.None);

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_EchoCommand_CapturesOutputAsync()
    {
        // Arrange
        ShellInfo shell = CreateCmdShell();
        List<string> output = [];

        // Act
        await CommandExecutionHelper.ExecuteAsync(
            "echo hello world",
            shell,
            Directory.GetCurrentDirectory(),
            output.Add,
            CancellationToken.None);

        // Assert
        output.Should().Contain(line => line.Contains("hello world"));
    }

    [Fact]
    public async Task ExecuteAsync_FailingCommand_ReturnsNonZeroExitCodeAsync()
    {
        // Arrange
        ShellInfo shell = CreateCmdShell();
        List<string> output = [];

        // Act
        int exitCode = await CommandExecutionHelper.ExecuteAsync(
            "exit 42",
            shell,
            Directory.GetCurrentDirectory(),
            output.Add,
            CancellationToken.None);

        // Assert
        exitCode.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteAsync_CapturesStdErrAsync()
    {
        // Arrange
        ShellInfo shell = CreateCmdShell();
        List<string> output = [];

        // Act
        await CommandExecutionHelper.ExecuteAsync(
            "echo error message 1>&2",
            shell,
            Directory.GetCurrentDirectory(),
            output.Add,
            CancellationToken.None);

        // Assert
        output.Should().Contain(line => line.Contains("error message"));
    }

    [Fact]
    public async Task ExecuteAsync_Cancellation_CancelsExecutionAsync()
    {
        // Arrange
        ShellInfo shell = CreateCmdShell();
        using CancellationTokenSource cts = new();
        TaskCompletionSource outputReceived = new();

        // Act – start a long-running command, wait for output, then cancel
        Task<int> task = CommandExecutionHelper.ExecuteAsync(
            "ping -n 300 127.0.0.1",
            shell,
            Directory.GetCurrentDirectory(),
            _ => outputReceived.TrySetResult(),
            cts.Token);

        // Wait until the process has started producing output before cancelling.
        await outputReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cts.Cancel();

        // Assert – the task should complete quickly (cancelled or killed) rather than running for 300 pings.
        Task completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5)));
        completed.Should().Be(task, because: "cancellation should terminate the process promptly");
        task.Status.Should().Be(TaskStatus.Canceled, because: "cancellation should terminate the process promptly");
    }

    [Fact]
    public async Task ExecuteAsync_RespectsWorkingDirectoryAsync()
    {
        // Arrange
        ShellInfo shell = CreateCmdShell();
        List<string> output = [];
        string tempDir = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);

        // Act
        await CommandExecutionHelper.ExecuteAsync(
            "cd",
            shell,
            tempDir,
            output.Add,
            CancellationToken.None);

        // Assert
        output.Should().Contain(line =>
            line.Equals(tempDir, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteAsync_MultiLineOutput_CapturesAllLinesAsync()
    {
        // Arrange
        ShellInfo shell = CreateCmdShell();
        List<string> output = [];

        // Act
        await CommandExecutionHelper.ExecuteAsync(
            "echo line1 & echo line2 & echo line3",
            shell,
            Directory.GetCurrentDirectory(),
            output.Add,
            CancellationToken.None);

        // Assert
        output.Should().Contain(l => l.Contains("line1"));
        output.Should().Contain(l => l.Contains("line2"));
        output.Should().Contain(l => l.Contains("line3"));
    }
}
