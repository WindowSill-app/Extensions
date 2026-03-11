namespace WindowSill.VideoHelper.Core;

/// <summary>
/// Pre-defined compression presets for common video compression scenarios.
/// </summary>
internal static class VideoPresets
{
    /// <summary>
    /// High quality, moderate size reduction using H.264.
    /// </summary>
    internal static VideoCompressionOptions Quality => new()
    {
        VideoCodec = "libx264",
        Crf = 18,
        Preset = "medium",
        AudioBitrateKbps = null,
    };

    /// <summary>
    /// Good balance of quality and file size using H.264.
    /// </summary>
    internal static VideoCompressionOptions Balanced => new()
    {
        VideoCodec = "libx264",
        Crf = 23,
        Preset = "medium",
        AudioBitrateKbps = 128,
    };

    /// <summary>
    /// Smaller file with noticeable quality loss using H.264.
    /// </summary>
    internal static VideoCompressionOptions SmallFile => new()
    {
        VideoCodec = "libx264",
        Crf = 28,
        Preset = "fast",
        AudioBitrateKbps = 96,
    };

    /// <summary>
    /// Maximum compression using H.265 (HEVC).
    /// </summary>
    internal static VideoCompressionOptions MaxCompression => new()
    {
        VideoCodec = "libx265",
        Crf = 28,
        Preset = "medium",
        AudioBitrateKbps = 96,
    };

    /// <summary>
    /// Gets all pre-defined compression presets with their display names.
    /// </summary>
    internal static IReadOnlyList<(string Name, string Description, VideoCompressionOptions Options)> AllPresets { get; } =
    [
        ("Quality", "High quality, moderate size reduction (H.264, CRF 18)", Quality),
        ("Balanced", "Good balance of quality and size (H.264, CRF 23)", Balanced),
        ("SmallFile", "Smaller file, noticeable quality loss (H.264, CRF 28)", SmallFile),
        ("MaxCompression", "Smallest file using HEVC (H.265, CRF 28)", MaxCompression),
    ];
}
