using System.Collections.Concurrent;
using System.Diagnostics;

namespace WindowSill.VideoHelper.Core;

/// <summary>
/// Detects available hardware-accelerated video encoders by probing FFmpeg.
/// </summary>
/// <remarks>
/// Supported GPU encoder families:
/// <list type="bullet">
///   <item>NVENC (NVIDIA): h264_nvenc, hevc_nvenc, av1_nvenc</item>
///   <item>AMF (AMD): h264_amf, hevc_amf, av1_amf</item>
///   <item>QSV (Intel): h264_qsv, hevc_qsv, av1_qsv</item>
/// </list>
/// Results are cached after the first probe per FFmpeg path.
/// </remarks>
internal sealed class GpuEncoderDetector
{
    private readonly string _ffmpegPath;
    private static readonly ConcurrentDictionary<string, HashSet<string>> _cache = new();

    internal GpuEncoderDetector(string ffmpegDirectory)
    {
        _ffmpegPath = FFmpegManager.GetFFmpegPath(ffmpegDirectory);
    }

    /// <summary>
    /// All known GPU encoder names, grouped by codec family.
    /// </summary>
    private static readonly string[] AllGpuEncoders =
    [
        // NVIDIA
        "h264_nvenc", "hevc_nvenc", "av1_nvenc",
        // AMD
        "h264_amf", "hevc_amf", "av1_amf",
        // Intel
        "h264_qsv", "hevc_qsv", "av1_qsv",
    ];

    /// <summary>
    /// Gets the set of available GPU encoders by running a short FFmpeg probe.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Set of available GPU encoder names (e.g. "h264_nvenc").</returns>
    internal async Task<HashSet<string>> DetectAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(_ffmpegPath, out HashSet<string>? cached))
        {
            return cached;
        }

        var available = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // First, get compiled-in encoders list
            var listStartInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = "-hide_banner -encoders",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var listProcess = Process.Start(listStartInfo);
            if (listProcess is null)
            {
                return available;
            }

            string output = await listProcess.StandardOutput.ReadToEndAsync(cancellationToken);
            _ = await listProcess.StandardError.ReadToEndAsync(cancellationToken);
            await listProcess.WaitForExitAsync(cancellationToken);

            // For each compiled-in GPU encoder, verify it actually works at runtime
            // by encoding a single synthetic frame
            foreach (string encoder in AllGpuEncoders)
            {
                if (output.Contains(encoder, StringComparison.OrdinalIgnoreCase))
                {
                    if (await ProbeEncoderAsync(encoder, cancellationToken))
                    {
                        available.Add(encoder);
                    }
                }
            }
        }
        catch
        {
            // Probe failed; return empty set — caller falls back to CPU
        }

        _cache[_ffmpegPath] = available;
        return available;
    }

    /// <summary>
    /// Tests whether a GPU encoder actually works at runtime by encoding a single synthetic frame.
    /// </summary>
    private async Task<bool> ProbeEncoderAsync(string encoder, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = $"-hide_banner -f lavfi -i color=black:s=256x256:d=0.04:r=25 -frames:v 1 -c:v {encoder} -f null -",
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

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            _ = await process.StandardOutput.ReadToEndAsync(cts.Token);
            _ = await process.StandardError.ReadToEndAsync(cts.Token);
            await process.WaitForExitAsync(cts.Token);

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Picks the best available GPU encoder for a given software codec, or returns the software codec if none available.
    /// </summary>
    /// <param name="softwareCodec">The software codec (e.g. "libx264", "libx265", "libsvtav1").</param>
    /// <param name="availableEncoders">Set of available GPU encoders from <see cref="DetectAsync"/>.</param>
    /// <returns>The GPU encoder name, or the original software codec if no GPU encoder is available.</returns>
    internal static string GetGpuEncoder(string softwareCodec, HashSet<string> availableEncoders)
    {
        // Map software codec → ordered GPU encoder preference (NVENC > AMF > QSV)
        string[] candidates = softwareCodec.ToLowerInvariant() switch
        {
            "libx264" => ["h264_nvenc", "h264_amf", "h264_qsv"],
            "libx265" => ["hevc_nvenc", "hevc_amf", "hevc_qsv"],
            "libsvtav1" => ["av1_nvenc", "av1_amf", "av1_qsv"],
            _ => [],
        };

        foreach (string candidate in candidates)
        {
            if (availableEncoders.Contains(candidate))
            {
                return candidate;
            }
        }

        return softwareCodec;
    }

    /// <summary>
    /// Builds the quality argument string appropriate for the chosen encoder.
    /// GPU encoders use different quality parameters than software encoders.
    /// </summary>
    /// <param name="encoder">The encoder name (e.g. "h264_nvenc", "libx264").</param>
    /// <param name="crf">The CRF value from user settings.</param>
    /// <param name="preset">The preset name from user settings.</param>
    /// <returns>FFmpeg arguments for quality and preset.</returns>
    internal static string BuildQualityArgs(string encoder, int crf, string preset)
    {
        // NVENC: uses -cq (constant quality) and -preset p1-p7
        if (encoder.EndsWith("_nvenc", StringComparison.OrdinalIgnoreCase))
        {
            string nvencPreset = preset.ToLowerInvariant() switch
            {
                "ultrafast" => "p1",
                "fast" => "p2",
                "medium" => "p4",
                "slow" => "p5",
                "veryslow" => "p7",
                _ => "p4",
            };
            return $"-rc constqp -qp {crf} -preset {nvencPreset}";
        }

        // AMF: uses -quality and -rc cqp with -qp_i/-qp_p
        if (encoder.EndsWith("_amf", StringComparison.OrdinalIgnoreCase))
        {
            string amfQuality = preset.ToLowerInvariant() switch
            {
                "ultrafast" => "speed",
                "fast" => "speed",
                "medium" => "balanced",
                "slow" => "quality",
                "veryslow" => "quality",
                _ => "balanced",
            };
            return $"-rc cqp -qp_i {crf} -qp_p {crf} -quality {amfQuality}";
        }

        // QSV: uses -global_quality and -preset
        if (encoder.EndsWith("_qsv", StringComparison.OrdinalIgnoreCase))
        {
            string qsvPreset = preset.ToLowerInvariant() switch
            {
                "ultrafast" => "veryfast",
                "fast" => "fast",
                "medium" => "medium",
                "slow" => "slow",
                "veryslow" => "veryslow",
                _ => "medium",
            };
            return $"-global_quality {crf} -preset {qsvPreset}";
        }

        // SVT-AV1: numeric preset
        if (string.Equals(encoder, "libsvtav1", StringComparison.OrdinalIgnoreCase))
        {
            string svtPreset = preset.ToLowerInvariant() switch
            {
                "ultrafast" => "12",
                "superfast" => "10",
                "veryfast" => "8",
                "faster" => "7",
                "fast" => "6",
                "medium" => "5",
                "slow" => "4",
                "slower" => "3",
                "veryslow" => "2",
                _ => "5",
            };
            return $"-crf {crf} -preset {svtPreset}";
        }

        // Software fallback (libx264, libx265, etc.)
        return $"-crf {crf} -preset {preset}";
    }

    /// <summary>
    /// Returns whether the given encoder is a GPU encoder.
    /// </summary>
    internal static bool IsGpuEncoder(string encoder)
        => encoder.EndsWith("_nvenc", StringComparison.OrdinalIgnoreCase)
           || encoder.EndsWith("_amf", StringComparison.OrdinalIgnoreCase)
           || encoder.EndsWith("_qsv", StringComparison.OrdinalIgnoreCase);
}
