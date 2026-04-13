using System.Text.RegularExpressions;

namespace WindowSill.InlineTerminal.Core;

/// <summary>
/// Provides utilities for stripping ANSI escape codes from terminal output.
/// </summary>
internal static partial class AnsiEscapeCodeHelper
{
    /// <summary>
    /// Removes all ANSI escape sequences (CSI, OSC, and single-character escapes) from the input string.
    /// </summary>
    internal static string StripAnsiEscapeCodes(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return AnsiEscapeCodeRegex().Replace(input, string.Empty);
    }

    // Matches:
    // - CSI sequences: ESC[ (or \x9B) followed by parameters and a final byte
    // - OSC sequences: ESC] ... ST (string terminator)
    // - Two-character escape sequences: ESC followed by a single character
    [GeneratedRegex(@"\x1B(?:\[[0-9;]*[A-Za-z]|\][^\x07]*(?:\x07|\x1B\\)|[A-Za-z])")]
    private static partial Regex AnsiEscapeCodeRegex();
}
