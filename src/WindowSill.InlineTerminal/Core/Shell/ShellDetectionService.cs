using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using Windows.Storage;
using Windows.Storage.FileProperties;
using WindowSill.API;
using Path = System.IO.Path;

namespace WindowSill.InlineTerminal.Core.Shell;

[Export]
internal sealed class ShellDetectionService
{
    private readonly TaskCompletionSource<IReadOnlyList<ShellInfo>> _shellsTcs = new();
    private int _detectionStarted;

    /// <summary>
    /// Returns the list of available shells, detecting them on first call and caching the result.
    /// Shell icons load asynchronously in the background via <see cref="TaskCompletionNotifier{TResult}"/>
    /// and render in the UI when ready.
    /// </summary>
    internal async ValueTask<IReadOnlyList<ShellInfo>> GetAvailableShellsAsync()
    {
        if (Interlocked.CompareExchange(ref _detectionStarted, 1, 0) == 0)
        {
            try
            {
                List<ShellInfo> shells = DetectShells();
                _shellsTcs.TrySetResult(shells);
            }
            catch (Exception ex)
            {
                _shellsTcs.TrySetException(ex);
            }
        }

        return await _shellsTcs.Task.ConfigureAwait(false);
    }

    /// <summary>
    /// Detects available shells. Icon loading is fired as background tasks per shell
    /// and exposed via <see cref="TaskCompletionNotifier{TResult}"/> for data-binding.
    /// </summary>
    private static List<ShellInfo> DetectShells()
    {
        List<ShellInfo> shells = [];

        // PowerShell 7 (pwsh)
        if (FindExecutableOnPath("pwsh") is { } pwshPath)
        {
            shells.Add(CreateShellInfo("PowerShell 7", pwshPath,
                escapeCommand: command => command.Replace("\"", "\\\""),
                buildArguments: command => $"-Command {command}",
                buildElevatedArguments: (command, outputFile) =>
                    $"-Command \"& {{ {command} }} *> '{outputFile}'\""));
        }

        // Windows PowerShell
        if (FindExecutableOnPath("powershell") is { } powershellPath)
        {
            shells.Add(CreateShellInfo("Windows PowerShell", powershellPath,
                escapeCommand: command => command.Replace("\"", "\\\""),
                buildArguments: command => $"-Command {command}",
                buildElevatedArguments: (command, outputFile) =>
                    $"-Command \"& {{ {command} }} *> '{outputFile}'\""));
        }

        // Command Prompt
        if (FindExecutableOnPath("cmd") is { } cmdPath)
        {
            shells.Add(CreateShellInfo("Command Prompt", cmdPath,
                escapeCommand: command => command.Replace("\"", "\\\""),
                buildArguments: command => $"/c {command}",
                buildElevatedArguments: (command, outputFile) =>
                    $"/c \"{command} > \"{outputFile}\" 2>&1\""));
        }

        // WSL distributions — only attempt if WSL is actually installed (not just the System32 stub).
        if (FindExecutableOnPath("wsl") is { } wslPath && IsWslInstalled())
        {
            Task<ImageSource?> wslIconTask = GetExecutableIconAsync(wslPath);

            foreach (string distro in DetectWslDistributions(wslPath))
            {
                shells.Add(CreateWslShellInfo(distro, wslPath, wslIconTask));
            }
        }

        return shells;
    }

    private static ShellInfo CreateShellInfo(
        string displayName,
        string executablePath,
        Func<string, string> escapeCommand,
        Func<string, string> buildArguments,
        Func<string, string, string> buildElevatedArguments)
    {
        return new ShellInfo(
            displayName,
            executablePath,
            escapeCommand,
            buildArguments,
            buildElevatedArguments,
            iconTask: GetExecutableIconAsync(executablePath));
    }

    private static ShellInfo CreateWslShellInfo(string distroName, string wslPath, Task<ImageSource?> wslIconTask)
    {
        return new ShellInfo(
            $"WSL · {distroName}",
            wslPath,
            escapeCommand: EscapeForBash,
            buildArguments: command => $"-d {distroName} -- bash -c {command}",
            buildElevatedArguments: (command, outputFile) =>
            {
                string wslOutputFile = WslPathConverter.ConvertToWslPath(outputFile);
                return $"-d {distroName} -- bash -c {EscapeForBash($"sudo {command} > '{wslOutputFile}' 2>&1")}";
            },
            wslDistroName: distroName,
            iconTask: wslIconTask);
    }

    /// <summary>
    /// Extracts the icon from an executable file using the Windows shell thumbnail API.
    /// </summary>
    private static async Task<ImageSource?> GetExecutableIconAsync(string executablePath)
    {
        try
        {
            return await ThreadHelper.RunOnUIThreadAsync(
                Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                async () =>
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(executablePath);

                    StorageItemThumbnail? thumbnail;
                    try
                    {
                        thumbnail
                            = await file.GetThumbnailAsync(
                                ThumbnailMode.SingleItem,
                                32,
                                ThumbnailOptions.UseCurrentScale);
                    }
                    catch (COMException ex) when (ex.HResult == unchecked((int)0x8000000A))
                    {
                        // E_PENDING: Shell thumbnail provider hasn't cached the icon yet.
                        return null;
                    }

                    if (thumbnail is null)
                    {
                        return null;
                    }

                    using (thumbnail)
                    {
                        var bitmap = new WriteableBitmap(
                            (int)thumbnail.OriginalWidth,
                            (int)thumbnail.OriginalHeight);

                        await bitmap.SetSourceAsync(thumbnail);
                        return (ImageSource)bitmap;
                    }
                });
        }
        catch
        {
            // No dispatcher available (e.g. unit tests) – icon is cosmetic, return null.
            return null;
        }
    }

    /// <summary>
    /// Escapes a command for safe embedding in a bash <c>-c</c> argument by wrapping in single quotes.
    /// </summary>
    private static string EscapeForBash(string command)
    {
        // Wrap in single quotes; escape existing single quotes as '\''
        string escaped = command.Replace("'", @"'\''");
        return $"'{escaped}'";
    }

    /// <summary>
    /// Runs <c>wsl --list --quiet</c> to enumerate installed WSL distributions.
    /// </summary>
    private static List<string> DetectWslDistributions(string wslPath)
    {
        List<string> distros = [];

        try
        {
            using Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = wslPath,
                    Arguments = "--list --quiet",
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = Encoding.Unicode, // wsl --list outputs UTF-16
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
            };

            process.Start();

            // Use WaitForExit with a timeout first to avoid hanging indefinitely
            // on machines where WSL is slow to respond (kernel boot, not initialized, etc.).
            // ReadToEnd() blocks until the process closes stdout, which has no timeout.
            if (!process.WaitForExit(5000))
            {
                try
                {
                    process.Kill();
                }
                catch { /* best effort */ }
                return distros;
            }

            if (process.ExitCode != 0)
            {
                return distros;
            }

            string output = process.StandardOutput.ReadToEnd();

            foreach (string line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                // wsl --list output may contain null characters; trim them.
                string distro = line.Trim().Trim('\0', '\r');

                if (!string.IsNullOrWhiteSpace(distro))
                {
                    distros.Add(distro);
                }
            }
        }
        catch (Exception)
        {
            // WSL not available or failed to enumerate.
        }

        return distros;
    }

    /// <summary>
    /// Checks whether WSL is actually installed by looking for the <c>lxss</c> registry key.
    /// This avoids spawning the System32 <c>wsl.exe</c> stub on machines where WSL is not enabled.
    /// </summary>
    private static bool IsWslInstalled()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Lxss");
            return key is not null;
        }
        catch
        {
            return false;
        }
    }

    private static string? FindExecutableOnPath(string exeName)
    {
        string fileName = OperatingSystem.IsWindows() ? $"{exeName}.exe" : exeName;

        string? pathVariable = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrEmpty(pathVariable))
        {
            return null;
        }

        foreach (string directory in pathVariable.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            string fullPath = Path.Combine(directory, fileName);

            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }
}
