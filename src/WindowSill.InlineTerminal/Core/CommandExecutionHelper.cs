using System.Diagnostics;
using WindowSill.API;
using WindowSill.InlineTerminal.Core.Shell;

namespace WindowSill.InlineTerminal.Core;

/// <summary>
/// Low-level process spawning and output capture for command execution.
/// </summary>
internal static class CommandExecutionHelper
{
    /// <summary>
    /// Executes a command in the specified shell, streaming output line by line.
    /// </summary>
    internal static async Task<int> ExecuteAsync(
        string command,
        ShellInfo shell,
        string workingDirectory,
        Action<string> onOutputLine,
        CancellationToken cancellationToken,
        bool skipEscaping = false)
    {
        string finalCommand = skipEscaping ? command : shell.EscapeCommand(command);

        using Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = shell.ExecutablePath,
                Arguments = BuildArgumentsWithWorkingDirectory(shell, finalCommand, workingDirectory),
                WorkingDirectory = shell.IsWsl ? string.Empty : workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true,
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                onOutputLine(AnsiEscapeCodeHelper.StripAnsiEscapeCodes(e.Data));
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                onOutputLine(AnsiEscapeCodeHelper.StripAnsiEscapeCodes(e.Data));
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await using CancellationTokenRegistration registration = RegisterCancellation(process, cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode;
    }

    /// <summary>
    /// Executes a command with elevated privileges (UAC), capturing output via a temporary file.
    /// </summary>
    internal static async Task<int> ExecuteElevatedAsync(
        string command,
        ShellInfo shell,
        string workingDirectory,
        Action<string> onOutputLine,
        IPluginInfo pluginInfo,
        CancellationToken cancellationToken,
        bool skipEscaping = false)
    {
        string finalCommand = skipEscaping ? command : shell.EscapeCommand(command);

        string tempOutputFile
            = System.IO.Path.Combine(
                pluginInfo.GetPluginTempFolder(),
                $"windowsill-terminal-{Guid.NewGuid():N}.log");

        using Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = shell.ExecutablePath,
                Arguments = BuildElevatedArgumentsWithWorkingDirectory(shell, finalCommand, workingDirectory, tempOutputFile),
                WorkingDirectory = shell.IsWsl ? string.Empty : workingDirectory,
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
            },
            EnableRaisingEvents = true,
        };

        process.Start();

        await using CancellationTokenRegistration registration = RegisterCancellation(process, cancellationToken);

        Task processWait = process.WaitForExitAsync(cancellationToken);
        Task tailTask = TailReadFileAsync(tempOutputFile, onOutputLine, processWait, cancellationToken);

        try
        {
            await Task.WhenAll(processWait, tailTask).ConfigureAwait(false);
            return process.ExitCode;
        }
        finally
        {
            try
            {
                File.Delete(tempOutputFile);
            }
            catch { /* best effort */ }
        }
    }

    private static CancellationTokenRegistration RegisterCancellation(
        Process process,
        CancellationToken cancellationToken)
    {
        return cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
                // Process already exited.
            }
        });
    }

    private static async Task TailReadFileAsync(
        string filePath,
        Action<string> onOutputLine,
        Task processExitTask,
        CancellationToken cancellationToken)
    {
        long lastPosition = 0;

        for (int i = 0; i < 50 && !File.Exists(filePath); i++)
        {
            if (cancellationToken.IsCancellationRequested || processExitTask.IsCompleted)
            {
                break;
            }

            await Task.Delay(100, CancellationToken.None).ConfigureAwait(false);
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    stream.Seek(lastPosition, SeekOrigin.Begin);
                    using var reader = new StreamReader(stream);

                    while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
                    {
                        onOutputLine(AnsiEscapeCodeHelper.StripAnsiEscapeCodes(line));
                    }

                    lastPosition = stream.Position;
                }
            }
            catch (IOException)
            {
                // File may be locked momentarily; retry.
            }

            if (processExitTask.IsCompleted)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        stream.Seek(lastPosition, SeekOrigin.Begin);
                        using var reader = new StreamReader(stream);

                        while (await reader.ReadLineAsync(CancellationToken.None).ConfigureAwait(false) is { } line)
                        {
                            onOutputLine(AnsiEscapeCodeHelper.StripAnsiEscapeCodes(line));
                        }
                    }
                }
                catch (IOException)
                {
                    // Best effort.
                }

                break;
            }

            await Task.Delay(250, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private static string BuildArgumentsWithWorkingDirectory(ShellInfo shell, string command, string workingDirectory)
    {
        if (shell.IsWsl && !string.IsNullOrEmpty(workingDirectory))
        {
            string wslPath = WslPathConverter.ConvertToWslPath(workingDirectory);
            return $"--cd {wslPath} {shell.BuildArguments(command)}";
        }

        return shell.BuildArguments(command);
    }

    private static string BuildElevatedArgumentsWithWorkingDirectory(ShellInfo shell, string command, string workingDirectory, string outputFilePath)
    {
        if (shell.IsWsl && !string.IsNullOrEmpty(workingDirectory))
        {
            string wslPath = WslPathConverter.ConvertToWslPath(workingDirectory);
            return $"--cd {wslPath} {shell.BuildElevatedArguments(command, outputFilePath)}";
        }

        return shell.BuildElevatedArguments(command, outputFilePath);
    }
}
