using System.Diagnostics;

namespace WindowSill.UniversalCommands.Core;

internal static class PowerShellHelper
{
    /// <summary>
    /// Executes a PowerShell command asynchronously.
    /// Tries <c>pwsh.exe</c> (PowerShell 7+) first, then falls back to <c>powershell.exe</c>.
    /// </summary>
    /// <param name="command">The PowerShell command to execute.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal static async Task ExecuteAsync(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        string executable = FindPowerShellExecutable();

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = $"-NoProfile -NonInteractive -Command \"{EscapeCommand(command)}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process is not null)
            {
                await process.WaitForExitAsync();
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Swallow execution errors — the command may reference unavailable tools.
            Debug.WriteLine($"PowerShell execution failed: {ex.Message}");
        }
    }

    private static string FindPowerShellExecutable()
    {
        // Prefer pwsh (PowerShell 7+) if available.
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "pwsh",
                Arguments = "-Version",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            using var process = Process.Start(psi);
            process?.WaitForExit(2000);

            if (process is { ExitCode: 0 })
            {
                return "pwsh";
            }
        }
        catch
        {
            // pwsh not found.
        }

        return "powershell";
    }

    private static string EscapeCommand(string command)
    {
        // Escape double quotes for the command-line argument.
        return command.Replace("\"", "\\\"");
    }
}
