using System.IO.Compression;
using System.Runtime.InteropServices;

using Path = System.IO.Path;

namespace WindowSill.VideoHelper.Core;

/// <summary>
/// Manages FFmpeg binary downloading, extraction, and location.
/// </summary>
/// <remarks>
/// FFmpeg binaries are downloaded from BtbN's FFmpeg-Builds on GitHub (GPL build).
/// Binaries are cached locally in the plugin's data folder.
/// </remarks>
internal static class FFmpegManager
{
    /// <summary>
    /// Gets the FFmpeg download URL based on the current system architecture.
    /// </summary>
    /// <returns>Download URL for either ARM64 or x64 FFmpeg build.</returns>
    private static string GetFFmpegDownloadUrl()
    {
        return RuntimeInformation.ProcessArchitecture == Architecture.Arm64
            ? "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-winarm64-gpl.zip"
            : "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";
    }

    /// <summary>
    /// Checks whether FFmpeg binaries are available in the specified directory.
    /// </summary>
    /// <param name="ffmpegDirectory">Directory where FFmpeg should be stored.</param>
    /// <returns>True if both ffmpeg.exe and ffprobe.exe exist; otherwise false.</returns>
    internal static bool FFmpegExists(string ffmpegDirectory)
    {
        string ffmpegPath = Path.Combine(ffmpegDirectory, Constants.FFmpegExecutable);
        string ffprobePath = Path.Combine(ffmpegDirectory, Constants.FFprobeExecutable);
        return File.Exists(ffmpegPath) && File.Exists(ffprobePath);
    }

    /// <summary>
    /// Gets the expected FFmpeg directory path within the plugin data folder.
    /// </summary>
    /// <param name="pluginDataFolder">The plugin's data folder path.</param>
    /// <returns>The path to the FFmpeg subdirectory.</returns>
    internal static string GetFFmpegDirectory(string pluginDataFolder)
        => Path.Combine(pluginDataFolder, Constants.FFmpegFolderName);

    /// <summary>
    /// Gets the path to the FFmpeg executable.
    /// </summary>
    /// <param name="ffmpegDirectory">Directory where FFmpeg is stored.</param>
    /// <returns>Full path to ffmpeg.exe.</returns>
    internal static string GetFFmpegPath(string ffmpegDirectory)
        => Path.Combine(ffmpegDirectory, Constants.FFmpegExecutable);

    /// <summary>
    /// Gets the path to the FFprobe executable.
    /// </summary>
    /// <param name="ffmpegDirectory">Directory where FFmpeg is stored.</param>
    /// <returns>Full path to ffprobe.exe.</returns>
    internal static string GetFFprobePath(string ffmpegDirectory)
        => Path.Combine(ffmpegDirectory, Constants.FFprobeExecutable);

    /// <summary>
    /// Downloads and extracts FFmpeg binaries if they don't already exist locally.
    /// </summary>
    /// <param name="ffmpegDirectory">Directory to store the FFmpeg binaries.</param>
    /// <param name="progress">Optional progress reporter (0.0 to 1.0). First 90% is download, last 10% is extraction.</param>
    /// <param name="cancellationToken">Token to cancel the download operation.</param>
    /// <remarks>
    /// If the download is cancelled or fails, any partially downloaded files are deleted.
    /// </remarks>
    internal static async Task DownloadFFmpegAsync(
        string ffmpegDirectory,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(ffmpegDirectory);

        string zipPath = Path.Combine(ffmpegDirectory, "ffmpeg.zip");

        try
        {
            // Download the zip (90% of progress)
            await DownloadFileAsync(
                GetFFmpegDownloadUrl(),
                zipPath,
                progress is not null ? new Progress<double>(p => progress.Report(p * 0.9)) : null,
                cancellationToken).ConfigureAwait(false);

            // Extract only ffmpeg.exe and ffprobe.exe (last 10% of progress)
            progress?.Report(0.9);
            await ExtractFFmpegBinariesAsync(zipPath, ffmpegDirectory, cancellationToken).ConfigureAwait(false);
            progress?.Report(1.0);
        }
        catch
        {
            CleanupPartialDownload(ffmpegDirectory);
            throw;
        }
        finally
        {
            TryDeleteFile(zipPath);
        }
    }

    /// <summary>
    /// Deletes any partially downloaded FFmpeg files from the specified directory.
    /// </summary>
    /// <param name="ffmpegDirectory">Directory containing the FFmpeg files.</param>
    internal static void CleanupPartialDownload(string ffmpegDirectory)
    {
        TryDeleteFile(Path.Combine(ffmpegDirectory, Constants.FFmpegExecutable));
        TryDeleteFile(Path.Combine(ffmpegDirectory, Constants.FFprobeExecutable));
        TryDeleteFile(Path.Combine(ffmpegDirectory, "ffmpeg.zip"));
    }

    private static async Task ExtractFFmpegBinariesAsync(
        string zipPath,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            using ZipArchive archive = ZipFile.OpenRead(zipPath);
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string entryName = entry.Name;
                if (string.Equals(entryName, Constants.FFmpegExecutable, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(entryName, Constants.FFprobeExecutable, StringComparison.OrdinalIgnoreCase))
                {
                    string destinationPath = Path.Combine(outputDirectory, entryName);
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    private static void TryDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (IOException)
        {
            // Ignore deletion failures
        }
    }

    /// <remarks>
    /// Every await in this method deliberately uses <c>ConfigureAwait(false)</c>. The download is
    /// started from the UI thread, which owns a <c>DispatcherQueueSynchronizationContext</c>; without
    /// this, each of the thousands of read/write continuations would be marshalled back to the UI
    /// thread, and the download could not make progress at all if that thread ever stalled.
    /// </remarks>
    private static async Task DownloadFileAsync(
        string url,
        string destinationPath,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        using var handler = new HttpClientHandler { AllowAutoRedirect = true };
        using var client = new HttpClient(handler);
        using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        long? totalBytes = response.Content.Headers.ContentLength;
        Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (contentStream.ConfigureAwait(false))
        {
            var fileStream = new FileStream(
                destinationPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            await using (fileStream.ConfigureAwait(false))
            {
                byte[] buffer = new byte[81920];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                    totalRead += bytesRead;

                    if (totalBytes.HasValue)
                    {
                        progress?.Report((double)totalRead / totalBytes.Value);
                    }
                }
            }
        }
    }
}
