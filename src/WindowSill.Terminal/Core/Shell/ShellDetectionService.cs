using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Text;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using WindowSill.API;
using Path = System.IO.Path;

namespace WindowSill.Terminal.Core.Shell;

[Export]
internal sealed class ShellDetectionService
{
    private IReadOnlyList<ShellInfo>? _cachedShells;

    /// <summary>
    /// Returns the list of available shells, detecting them on first call and caching the result.
    /// </summary>
    internal async ValueTask<IReadOnlyList<ShellInfo>> GetAvailableShellsAsync()
    {
        return _cachedShells ??= await DetectShellsAsync();
    }

    private static async Task<List<ShellInfo>> DetectShellsAsync()
    {
        List<ShellInfo> shells = [];

        // PowerShell 7 (pwsh)
        if (FindExecutableOnPath("pwsh") is { } pwshPath)
        {
            shells.Add(await CreatePowerShellInfoAsync("PowerShell 7", pwshPath));
        }

        // Windows PowerShell
        if (FindExecutableOnPath("powershell") is { } powershellPath)
        {
            shells.Add(await CreatePowerShellInfoAsync("Windows PowerShell", powershellPath));
        }

        // Command Prompt
        if (FindExecutableOnPath("cmd") is { } cmdPath)
        {
            shells.Add(new ShellInfo(
                "Command Prompt",
                cmdPath,
                escapeCommand: command => command.Replace("\"", "\\\""),
                buildArguments: command => $"/c {command}",
                buildElevatedArguments: (command, outputFile) =>
                    $"/c \"{command} > \"{outputFile}\" 2>&1\"",
                icon: await GetExecutableIconAsync(cmdPath)));
        }

        // WSL distributions
        if (FindExecutableOnPath("wsl") is { } wslPath)
        {
            ImageSource? wslIcon = await GetExecutableIconAsync(wslPath);

            foreach (string distro in DetectWslDistributions(wslPath))
            {
                shells.Add(CreateWslShellInfo(distro, wslPath, wslIcon));
            }
        }

        return shells;
    }

    private static async Task<ShellInfo> CreatePowerShellInfoAsync(string displayName, string executablePath)
    {
        return new ShellInfo(
            displayName,
            executablePath,
            escapeCommand: command => command.Replace("\"", "\\\""),
            buildArguments: command => $"-Command {command}",
            buildElevatedArguments: (command, outputFile) =>
                $"-Command \"& {{ {command} }} *> '{outputFile}'\"",
            icon: await GetExecutableIconAsync(executablePath));
    }

    private static ShellInfo CreateWslShellInfo(string distroName, string wslPath, ImageSource? icon)
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
            icon: icon);
    }

    /// <summary>
    /// Extracts the icon from an executable file using the Windows shell thumbnail API.
    /// </summary>
    private static async Task<ImageSource?> GetExecutableIconAsync(string executablePath)
    {
        return await ThreadHelper.RunOnUIThreadAsync(
            Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
            async () =>
            {
                try
                {
                    StorageFile file = await StorageFile.GetFileFromPathAsync(executablePath);
                    StorageItemThumbnail? thumbnail = await file.GetThumbnailAsync(
                        ThumbnailMode.SingleItem,
                        32,
                        ThumbnailOptions.UseCurrentScale);

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
                }
                catch
                {
                    return null;
                }
            });
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

            string output = process.StandardOutput.ReadToEnd();

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
