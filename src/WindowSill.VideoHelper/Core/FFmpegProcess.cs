using System.Diagnostics;
using System.Globalization;

namespace WindowSill.VideoHelper.Core;

/// <summary>
/// Wraps FFmpeg process execution with progress reporting and cancellation support.
/// </summary>
internal sealed partial class FFmpegProcess
{
    private readonly string _ffmpegPath;
    private readonly string _ffprobePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="FFmpegProcess"/> class.
    /// </summary>
    /// <param name="ffmpegDirectory">Directory containing FFmpeg binaries.</param>
    internal FFmpegProcess(string ffmpegDirectory)
    {
        _ffmpegPath = FFmpegManager.GetFFmpegPath(ffmpegDirectory);
        _ffprobePath = FFmpegManager.GetFFprobePath(ffmpegDirectory);
    }

    /// <summary>
    /// Gets the duration of a video file using FFprobe.
    /// </summary>
    /// <param name="filePath">Path to the video file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Duration of the video, or null if it cannot be determined.</returns>
    internal async Task<TimeSpan?> GetDurationAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _ffprobePath,
            Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{filePath}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return null;
        }

        string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (double.TryParse(output.Trim(), CultureInfo.InvariantCulture, out double seconds))
        {
            return TimeSpan.FromSeconds(seconds);
        }

        return null;
    }

    /// <summary>
    /// Runs FFmpeg with the specified arguments and reports progress based on video duration.
    /// </summary>
    /// <param name="arguments">FFmpeg command-line arguments.</param>
    /// <param name="totalDuration">Total duration of the source video for progress calculation.</param>
    /// <param name="progress">Optional progress reporter (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if FFmpeg completed successfully; otherwise false.</returns>
    internal async Task<bool> RunAsync(
        string arguments,
        TimeSpan? totalDuration,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = $"-y -progress pipe:1 -nostats {arguments}",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return false;
        }

        using CancellationTokenRegistration registration = cancellationToken.Register(() =>
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
                // Process already exited
            }
        });

        // Read stdout and stderr concurrently to prevent pipe deadlock
        Task readStdoutTask = ReadProgressOutputAsync(process.StandardOutput, totalDuration, progress, cancellationToken);
        Task<string> readStderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await Task.WhenAll(readStdoutTask, readStderrTask);

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode == 0;
    }

    private static async Task ReadProgressOutputAsync(
        StreamReader reader,
        TimeSpan? totalDuration,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        double totalSeconds = totalDuration?.TotalSeconds ?? 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            string? line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            if (totalSeconds > 0 && line.StartsWith("out_time_us=", StringComparison.Ordinal))
            {
                string value = line["out_time_us=".Length..];
                if (long.TryParse(value, CultureInfo.InvariantCulture, out long microseconds) && microseconds >= 0)
                {
                    double currentSeconds = microseconds / 1_000_000.0;
                    double percent = Math.Min(currentSeconds / totalSeconds, 1.0);
                    progress?.Report(percent);
                }
            }
        }
    }
}
