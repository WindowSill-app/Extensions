using System.Globalization;

namespace WindowSill.VideoHelper.Core;

/// <summary>
/// Compresses video files using FFmpeg, preferring GPU-accelerated encoders when available.
/// </summary>
internal sealed class FFmpegVideoCompressor : IVideoCompressor
{
    private readonly FFmpegProcess _ffmpeg;
    private readonly GpuEncoderDetector _gpuDetector;

    /// <summary>
    /// Initializes a new instance of the <see cref="FFmpegVideoCompressor"/> class.
    /// </summary>
    /// <param name="ffmpegDirectory">Directory containing FFmpeg binaries.</param>
    internal FFmpegVideoCompressor(string ffmpegDirectory)
    {
        _ffmpeg = new FFmpegProcess(ffmpegDirectory);
        _gpuDetector = new GpuEncoderDetector(ffmpegDirectory);
    }

    /// <inheritdoc />
    public async Task<bool> CompressAsync(
        string sourcePath,
        string outputPath,
        VideoCompressionOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        TimeSpan? duration = await _ffmpeg.GetDurationAsync(sourcePath, cancellationToken);

        // Detect GPU encoders and pick the best one for the requested codec
        HashSet<string> gpuEncoders = await _gpuDetector.DetectAsync(cancellationToken);
        string encoder = GpuEncoderDetector.GetGpuEncoder(options.VideoCodec, gpuEncoders);
        string qualityArgs = GpuEncoderDetector.BuildQualityArgs(encoder, options.Crf, options.Preset, options.VideoBitrateKbps);

        string audioArgs = options.AudioBitrateKbps.HasValue
            ? $"-b:a {options.AudioBitrateKbps.Value}k"
            : "-c:a copy";

        string videoFilterArgs = BuildVideoFilterArgs(options);
        string frameRateArgs = options.MaxFrameRate.HasValue
            ? string.Create(CultureInfo.InvariantCulture, $"-r {options.MaxFrameRate.Value}")
            : string.Empty;

        string arguments = string.Create(
            CultureInfo.InvariantCulture,
            $"-i \"{sourcePath}\" -c:v {encoder} {qualityArgs} {audioArgs} {videoFilterArgs} {frameRateArgs} \"{outputPath}\"");

        bool success = await _ffmpeg.RunAsync(arguments, duration, progress, cancellationToken);

        // If GPU encoder failed, retry with the original software encoder
        if (!success && GpuEncoderDetector.IsGpuEncoder(encoder))
        {
            TryDeleteFile(outputPath);
            string swQualityArgs = GpuEncoderDetector.BuildQualityArgs(options.VideoCodec, options.Crf, options.Preset, options.VideoBitrateKbps);
            string swArguments = string.Create(
                CultureInfo.InvariantCulture,
                $"-i \"{sourcePath}\" -c:v {options.VideoCodec} {swQualityArgs} {audioArgs} {videoFilterArgs} {frameRateArgs} \"{outputPath}\"");

            progress?.Report(0);
            success = await _ffmpeg.RunAsync(swArguments, duration, progress, cancellationToken);
        }

        return success;
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch { }
    }

    private static string BuildVideoFilterArgs(VideoCompressionOptions options)
    {
        if (!options.ResolutionHeight.HasValue)
        {
            return string.Empty;
        }

        // Scale to target height, auto-calculate width preserving aspect ratio.
        // -2 ensures the width is divisible by 2 (required by most encoders).
        return string.Create(
            CultureInfo.InvariantCulture,
            $"-vf scale=-2:{options.ResolutionHeight.Value}");
    }
}
