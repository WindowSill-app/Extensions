using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using WindowSill.API;

namespace WindowSill.DevToys.Core;

internal static partial class Base64Helper
{
    private static readonly ILogger logger = typeof(Base64Helper).Log();

    internal static bool IsBase64DataStrict(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return false;
        }

        ReadOnlySpan<char> span = data.AsSpan().Trim();

        if (span.Length % 4 != 0)
        {
            return false;
        }

        // Check for invalid characters using span
        for (int i = 0; i < span.Length; i++)
        {
            char c = span[i];
            if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') ||
                  (c >= '0' && c <= '9') || c == '+' || c == '/' || c == '='))
            {
                return false;
            }
        }

        // Check padding position
        int equalIndex = span.IndexOf('=');
        int length = span.Length;

        if (!(equalIndex == -1 || equalIndex == length - 1 ||
              (equalIndex == length - 2 && span[length - 1] == '=')))
        {
            return false;
        }

        byte[] decodedData;
        try
        {
            // Convert span to string only when necessary for Convert.FromBase64String
            decodedData = Convert.FromBase64String(span.ToString());
        }
        catch (Exception)
        {
            return false;
        }

        // Check for special chars using span over decoded bytes
        ReadOnlySpan<byte> decodedSpan = decodedData.AsSpan();
        Span<char> charBuffer = stackalloc char[512]; // Stack buffer for small strings

        int totalChars = 0;
        int bytesProcessed = 0;

        while (bytesProcessed < decodedSpan.Length)
        {
            int bytesToProcess = Math.Min(charBuffer.Length, decodedSpan.Length - bytesProcessed);
            int charsDecoded = Encoding.UTF8.GetChars(decodedSpan.Slice(bytesProcessed, bytesToProcess), charBuffer);

            for (int i = 0; i < charsDecoded; i++)
            {
                char current = charBuffer[i];
                if (current == 65533) // Replacement character
                {
                    return false;
                }

                if (!(current == 0x9 || current == 0xA || current == 0xD ||
                      (current >= 0x20 && current <= 0xD7FF) ||
                      (current >= 0xE000 && current <= 0xFFFD) ||
                      (current >= 0x10000 && current <= 0x10FFFF)))
                {
                    return false;
                }
            }

            bytesProcessed += bytesToProcess;
            totalChars += charsDecoded;
        }

        return true;
    }

    internal static string FromBase64ToText(string? data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return string.Empty;
        }

        ReadOnlySpan<char> span = data.AsSpan();

        // Handle padding using span operations
        Span<char> paddedData = stackalloc char[span.Length + 4]; // Max possible padding
        span.CopyTo(paddedData);
        int actualLength = span.Length;

        int remainder = actualLength % 4;
        if (remainder > 0)
        {
            int padding = 4 - remainder;
            paddedData.Slice(actualLength, padding).Fill('=');
            actualLength += padding;
        }

        try
        {
            byte[] decodedData = Convert.FromBase64String(paddedData.Slice(0, actualLength).ToString());

            if (decodedData == null || decodedData.Length == 0)
            {
                return string.Empty;
            }

            Encoding encoder = new UTF8Encoding(true);
            ReadOnlySpan<byte> dataSpan = decodedData.AsSpan();

            // Check for BOM
            ReadOnlySpan<byte> preamble = encoder.GetPreamble();
            bool hasBom = dataSpan.StartsWith(preamble);

            if (hasBom)
            {
                // Handle BOM case - need to create string for Unicode conversion
                string bomChar = Encoding.Unicode.GetString(preamble.ToArray(), 0, 1);
                string decoded = encoder.GetString(decodedData);
                return bomChar + decoded;
            }
            else
            {
                return encoder.GetString(decodedData);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException || ex is FormatException)
        {
            return string.Empty;
        }
        catch (Exception ex)
        {
            logger.LogError(1, ex, "Failed to decode Base64t to text.");
            return string.Empty;
        }
    }

    [GeneratedRegex("[^A-Z0-9+/=]", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex Base64Regex();
}
