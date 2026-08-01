using System.Text;

namespace WindowSill.FileHelper.Core;

/// <summary>
/// How a text file is encoded on disk, as far as FileHelper distinguishes.
/// </summary>
internal enum TextEncodingKind
{
    /// <summary>UTF-8 without a byte order mark — the modern default.</summary>
    Utf8,

    /// <summary>UTF-8 with a byte order mark.</summary>
    Utf8Bom,

    /// <summary>UTF-16 little endian, with a byte order mark.</summary>
    Utf16,

    /// <summary>The system's legacy single-byte code page.</summary>
    Ansi,
}

/// <summary>
/// What a text file's line breaks look like.
/// </summary>
internal enum LineEndingKind
{
    /// <summary>No line breaks were found.</summary>
    None,

    /// <summary>Windows-style carriage return + line feed.</summary>
    Crlf,

    /// <summary>Unix-style line feed.</summary>
    Lf,

    /// <summary>A file containing both styles.</summary>
    Mixed,
}

/// <summary>
/// Everything FileHelper reports about a text file: how it is encoded, how its lines end, and how much of it there
/// is.
/// </summary>
/// <param name="Encoding">The detected encoding.</param>
/// <param name="LineEndings">The detected line-ending style.</param>
/// <param name="LineCount">Number of lines.</param>
/// <param name="WordCount">Number of whitespace-separated words.</param>
/// <param name="CharacterCount">Number of characters, excluding line breaks.</param>
internal sealed record TextFileInfo(
    TextEncodingKind Encoding,
    LineEndingKind LineEndings,
    int LineCount,
    int WordCount,
    int CharacterCount);

/// <summary>
/// Reads text files without being told how they are encoded, and reports what it found.
/// </summary>
/// <remarks>
/// Detection is byte-order-mark first, then a strict UTF-8 decode: strict decoding fails on byte sequences that are
/// not valid UTF-8, which is what distinguishes a UTF-8 file from a legacy single-byte one. Guessing wrong would
/// silently corrupt accented characters on rewrite, so the fallback is only taken once UTF-8 has been ruled out.
/// </remarks>
internal static class TextFileReader
{
    /// <summary>
    /// Reads a text file, detecting its encoding.
    /// </summary>
    /// <param name="sourcePath">Path to the file.</param>
    /// <returns>The file's content and the encoding it was stored in.</returns>
    internal static (string Content, TextEncodingKind Encoding) Read(string sourcePath)
    {
        byte[] bytes = File.ReadAllBytes(sourcePath);

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return (new UTF8Encoding(false).GetString(bytes, 3, bytes.Length - 3), TextEncodingKind.Utf8Bom);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return (Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2), TextEncodingKind.Utf16);
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return (Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2), TextEncodingKind.Utf16);
        }

        try
        {
            var strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            return (strict.GetString(bytes), TextEncodingKind.Utf8);
        }
        catch (DecoderFallbackException)
        {
            // Not valid UTF-8, so treat it as the system's legacy code page.
            return (GetAnsiEncoding().GetString(bytes), TextEncodingKind.Ansi);
        }
    }

    /// <summary>
    /// Describes a text file without loading it more than once.
    /// </summary>
    /// <param name="sourcePath">Path to the file.</param>
    /// <returns>The file's encoding, line-ending style and counts.</returns>
    internal static TextFileInfo Describe(string sourcePath)
    {
        (string content, TextEncodingKind encoding) = Read(sourcePath);

        int crlf = 0;
        int lf = 0;
        for (int i = 0; i < content.Length; i++)
        {
            if (content[i] == '\n')
            {
                if (i > 0 && content[i - 1] == '\r')
                {
                    crlf++;
                }
                else
                {
                    lf++;
                }
            }
        }

        LineEndingKind lineEndings = (crlf, lf) switch
        {
            (0, 0) => LineEndingKind.None,
            (> 0, 0) => LineEndingKind.Crlf,
            (0, > 0) => LineEndingKind.Lf,
            _ => LineEndingKind.Mixed,
        };

        int breaks = crlf + lf;

        // A trailing newline terminates the last line rather than starting an empty one.
        int lineCount = content.Length == 0
            ? 0
            : breaks + (content.EndsWith('\n') ? 0 : 1);

        int words = content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        int characters = content.Count(c => c is not ('\r' or '\n'));

        return new TextFileInfo(encoding, lineEndings, lineCount, words, characters);
    }

    /// <summary>
    /// Gets the <see cref="Encoding"/> used to write a given kind.
    /// </summary>
    internal static Encoding ToEncoding(TextEncodingKind kind) => kind switch
    {
        TextEncodingKind.Utf8 => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        TextEncodingKind.Utf8Bom => new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
        TextEncodingKind.Utf16 => new UnicodeEncoding(bigEndian: false, byteOrderMark: true),
        TextEncodingKind.Ansi => GetAnsiEncoding(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported text encoding."),
    };

    /// <summary>
    /// Rewrites the line breaks in <paramref name="content"/> to a single style.
    /// </summary>
    /// <param name="content">The text to normalize.</param>
    /// <param name="lineEnding">The line break to use.</param>
    internal static string NormalizeLineEndings(string content, LineEndingKind lineEnding)
    {
        string replacement = lineEnding switch
        {
            LineEndingKind.Crlf => "\r\n",
            LineEndingKind.Lf => "\n",
            _ => throw new ArgumentOutOfRangeException(nameof(lineEnding), lineEnding, "Not a writable line ending."),
        };

        // Collapse to bare line feeds first so mixed input converges on one style.
        return content.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", replacement);
    }

    /// <summary>
    /// Gets the system's legacy single-byte code page, falling back to Latin-1 where it is unavailable.
    /// </summary>
    private static Encoding GetAnsiEncoding()
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(0);
        }
        catch (Exception)
        {
            return Encoding.Latin1;
        }
    }
}
