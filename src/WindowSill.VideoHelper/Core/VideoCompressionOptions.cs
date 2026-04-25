namespace WindowSill.VideoHelper.Core;

/// <summary>
/// Compression options for video files.
/// </summary>
internal sealed class VideoCompressionOptions
{
    /// <summary>
    /// Gets or sets the video codec to use (e.g., "libx264", "libx265", "libsvtav1").
    /// </summary>
    internal string VideoCodec { get; set; } = "libx264";

    /// <summary>
    /// Gets or sets the Constant Rate Factor (CRF). Lower values mean better quality.
    /// Typical range: 0-51 for H.264/H.265, 0-63 for AV1.
    /// </summary>
    internal int Crf { get; set; } = 23;

    /// <summary>
    /// Gets or sets the encoding speed preset (e.g., "ultrafast", "fast", "medium", "slow", "veryslow").
    /// </summary>
    internal string Preset { get; set; } = "medium";

    /// <summary>
    /// Gets or sets the audio bitrate in kbps (e.g., 128, 192, 256). Null to copy audio as-is.
    /// </summary>
    internal int? AudioBitrateKbps { get; set; }

    /// <summary>
    /// Gets or sets the target resolution height in pixels (e.g., 720, 1080). Null to keep original resolution.
    /// Width is calculated automatically to preserve aspect ratio.
    /// </summary>
    internal int? ResolutionHeight { get; set; }

    /// <summary>
    /// Gets or sets the maximum frame rate in fps (e.g., 24, 30, 60). Null to keep original frame rate.
    /// </summary>
    internal int? MaxFrameRate { get; set; }

    /// <summary>
    /// Gets or sets the target video bitrate in kbps for constant bitrate (CBR) mode.
    /// When set, CBR is used instead of CRF. Null to use CRF mode (the default).
    /// </summary>
    internal int? VideoBitrateKbps { get; set; }
}
