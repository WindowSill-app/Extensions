namespace WindowSill.VideoHelper;

internal static class Constants
{
    /// <summary>
    /// Video file extensions supported for input.
    /// </summary>
    internal static readonly string[] SupportedExtensions
        = [".mp4", ".mkv", ".avi", ".mov", ".webm", ".wmv", ".flv", ".m4v", ".ts", ".3gp"];

    /// <summary>
    /// FFmpeg binary folder name within the plugin data directory.
    /// </summary>
    internal const string FFmpegFolderName = "ffmpeg";

    /// <summary>
    /// FFmpeg executable name.
    /// </summary>
    internal const string FFmpegExecutable = "ffmpeg.exe";

    /// <summary>
    /// FFprobe executable name.
    /// </summary>
    internal const string FFprobeExecutable = "ffprobe.exe";
}
