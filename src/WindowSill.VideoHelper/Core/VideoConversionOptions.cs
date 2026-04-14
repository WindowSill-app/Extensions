namespace WindowSill.VideoHelper.Core;

/// <summary>
/// Conversion options for video files.
/// </summary>
internal sealed class VideoConversionOptions
{
    /// <summary>
    /// Gets or sets the output container format (e.g., "mp4", "mkv", "webm", "avi", "mov", "gif").
    /// </summary>
    internal string OutputFormat { get; set; } = "mp4";

    /// <summary>
    /// Gets or sets the video codec. Null for auto-selection based on container.
    /// </summary>
    internal string? VideoCodec { get; set; }

    /// <summary>
    /// Gets or sets the audio codec. Null for auto-selection based on container.
    /// </summary>
    internal string? AudioCodec { get; set; }

    /// <summary>
    /// Gets or sets whether to keep the audio track. False removes audio entirely.
    /// </summary>
    internal bool KeepAudio { get; set; } = true;

    /// <summary>
    /// Gets the recommended video codec for the current output format.
    /// </summary>
    internal string GetEffectiveVideoCodec()
        => VideoCodec ?? OutputFormat switch
        {
            "webm" => "libvpx-vp9",
            "gif" => "gif",
            _ => "libx264",
        };

    /// <summary>
    /// Gets the recommended audio codec for the current output format.
    /// </summary>
    internal string? GetEffectiveAudioCodec()
    {
        if (!KeepAudio || string.Equals(OutputFormat, "gif", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return AudioCodec ?? OutputFormat switch
        {
            "webm" => "libopus",
            "mkv" => "aac",
            "avi" => "mp3",
            _ => "aac",
        };
    }
}
