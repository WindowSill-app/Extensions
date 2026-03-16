using System.ComponentModel.Composition;
using System.Diagnostics;

namespace WindowSill.Terminal.Core;

/// <summary>
/// Executes commands via an external shell process.
/// </summary>
[Export(typeof(ICommandExecutionService))]
public sealed class CommandExecutionService : ICommandExecutionService
{
    /// <inheritdoc />
    public async Task<int> ExecuteAsync(
        string command,
        ShellInfo shell,
        string workingDirectory,
        Action<string> onOutputLine,
        CancellationToken cancellationToken)
    {
        using Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = shell.ExecutablePath,
                Arguments = $"{shell.ArgumentPrefix} {command}",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true,
        };

        TaskCompletionSource<int> exitTcs = new();

        process.Exited += (_, _) => exitTcs.TrySetResult(process.ExitCode);

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                onOutputLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                onOutputLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await using CancellationTokenRegistration registration = cancellationToken.Register(() =>
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

            exitTcs.TrySetCanceled(cancellationToken);
        });

        return await exitTcs.Task.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> ExecuteElevatedAsync(
        string command,
        ShellInfo shell,
        string workingDirectory,
        Action<string> onOutputLine,
        CancellationToken cancellationToken)
    {
        string tempOutputFile = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"windowsill-terminal-{Guid.NewGuid():N}.log");

        // Wrap the command so the shell writes all output to the temp file.
        // For pwsh/powershell: -Command "& { <command> } *> <file>"
        // For cmd: /c "<command> > <file> 2>&1"
        string wrappedArguments = shell.ArgumentPrefix == "/c"
            ? $"/c \"{command} > \"{tempOutputFile}\" 2>&1\""
            : $"{shell.ArgumentPrefix} \"& {{ {command} }} *> '{tempOutputFile}'\"";

        using Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = shell.ExecutablePath,
                Arguments = wrappedArguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
            },
            EnableRaisingEvents = true,
        };

        TaskCompletionSource<int> exitTcs = new();
        process.Exited += (_, _) => exitTcs.TrySetResult(process.ExitCode);

        process.Start();

        // Tail-read the temp file while the process runs.
        _ = TailReadFileAsync(tempOutputFile, onOutputLine, exitTcs.Task, cancellationToken);

        await using CancellationTokenRegistration registration = cancellationToken.Register(() =>
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

            exitTcs.TrySetCanceled(cancellationToken);
        });

        try
        {
            return await exitTcs.Task.ConfigureAwait(false);
        }
        finally
        {
            // Clean up temp file.
            try { File.Delete(tempOutputFile); } catch { /* best effort */ }
        }
    }

    private static async Task TailReadFileAsync(
        string filePath,
        Action<string> onOutputLine,
        Task processExitTask,
        CancellationToken cancellationToken)
    {
        long lastPosition = 0;

        // Wait briefly for the file to be created.
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
                        onOutputLine(line);
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
                // One final read to flush remaining output.
                try
                {
                    if (File.Exists(filePath))
                    {
                        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        stream.Seek(lastPosition, SeekOrigin.Begin);
                        using var reader = new StreamReader(stream);

                        while (await reader.ReadLineAsync(CancellationToken.None).ConfigureAwait(false) is { } line)
                        {
                            onOutputLine(line);
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
}
