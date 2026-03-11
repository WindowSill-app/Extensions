using System.Globalization;

namespace WindowSill.VideoHelper.Core;

/// <summary>
/// Converts video files to different formats using FFmpeg, preferring GPU-accelerated encoders when available.
/// </summary>
internal sealed class FFmpegVideoConverter : IVideoConverter
{
    private readonly FFmpegProcess _ffmpeg;
    private readonly GpuEncoderDetector _gpuDetector;

    /// <summary>
    /// Initializes a new instance of the <see cref="FFmpegVideoConverter"/> class.
    /// </summary>
    /// <param name="ffmpegDirectory">Directory containing FFmpeg binaries.</param>
    internal FFmpegVideoConverter(string ffmpegDirectory)
    {
        _ffmpeg = new FFmpegProcess(ffmpegDirectory);
        _gpuDetector = new GpuEncoderDetector(ffmpegDirectory);
    }

    /// <inheritdoc />
    public async Task<bool> ConvertAsync(
        string sourcePath,
        string outputPath,
        VideoConversionOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        TimeSpan? duration = await _ffmpeg.GetDurationAsync(sourcePath, cancellationToken);

        string softwareCodec = options.GetEffectiveVideoCodec();
        string? audioCodec = options.GetEffectiveAudioCodec();

        // Try GPU acceleration (not for GIF)
        string encoder = softwareCodec;
        if (!string.Equals(softwareCodec, "gif", StringComparison.OrdinalIgnoreCase))
        {
            HashSet<string> gpuEncoders = await _gpuDetector.DetectAsync(cancellationToken);
            encoder = GpuEncoderDetector.GetGpuEncoder(softwareCodec, gpuEncoders);
        }

        string audioArgs;
        if (!options.KeepAudio || string.Equals(options.OutputFormat, "gif", StringComparison.OrdinalIgnoreCase))
        {
            audioArgs = "-an";
        }
        else if (audioCodec is not null)
        {
            audioArgs = $"-c:a {audioCodec}";
        }
        else
        {
            audioArgs = "-c:a copy";
        }

        string gifArgs = string.Equals(options.OutputFormat, "gif", StringComparison.OrdinalIgnoreCase)
            ? "-vf \"fps=15,scale=480:-1:flags=lanczos\" -loop 0"
            : string.Empty;

        string arguments = string.Create(
            CultureInfo.InvariantCulture,
            $"-i \"{sourcePath}\" -c:v {encoder} {audioArgs} {gifArgs} \"{outputPath}\"");

        bool success = await _ffmpeg.RunAsync(arguments, duration, progress, cancellationToken);

        // If GPU encoder failed, retry with the original software encoder
        if (!success && GpuEncoderDetector.IsGpuEncoder(encoder))
        {
            TryDeleteFile(outputPath);
            string swArguments = string.Create(
                CultureInfo.InvariantCulture,
                $"-i \"{sourcePath}\" -c:v {softwareCodec} {audioArgs} {gifArgs} \"{outputPath}\"");

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
}
