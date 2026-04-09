using WindowSill.InlineTerminal.Parsers;
using Path = System.IO.Path;

namespace WindowSill.InlineTerminal.Core.Parsers;

/// <summary>
/// Parses Windows-style terminal command lines (PS, CMD) from selected text.
/// Coordinates with <see cref="BashPromptParser"/> for bash-style prompts.
/// </summary>
internal static class TerminalCommandParser
{
    private static readonly string[] executableExtensions = [".exe", ".cmd", ".bat", ".ps1", ".com"];

    /// <summary>
    /// Given the full text selected by the user, parses and returns the first inferred working directory.
    /// </summary>
    internal static string? GetFirstWorkingDirectory(string selectedText)
    {
        ReadOnlyMemory<char> selectedTextMemory = selectedText.AsMemory();
        WindowsHostCommandLineEntry? winEntry
            = ParseWindowsHostCommandLines(
                selectedTextMemory,
                shellDelimiter: "PS".AsMemory(),
                commandDelimiter: ">".AsMemory())
            .FirstOrDefault();

        return winEntry?.WorkingDirectory?.ToString() ?? BashPromptParser.GetWorkingDirectory(selectedText)?.ToString();
    }

    /// <summary>
    /// Given the full text selected by the user, parses and returns the first inferred terminal command from the text.
    /// </summary>
    internal static string? GetFirstTerminalCommand(string selectedText)
    {
        ReadOnlyMemory<char> selectedTextMemory = selectedText.AsMemory();
        WindowsHostCommandLineEntry? winEntry
            = ParseWindowsHostCommandLines(
                selectedTextMemory,
                shellDelimiter: "PS".AsMemory(),
                commandDelimiter: ">".AsMemory())
            .FirstOrDefault();

        return winEntry?.TerminalCommand.ToString() ?? BashPromptParser.GetTerminalCommand(selectedText)?.ToString();
    }

    /// <summary>
    /// Parses the selected text and separates out individual Windows host shell invocations.
    /// </summary>
    /// <remarks>
    /// Does not handle multiline commands, to be added.
    /// </remarks>
    private static List<WindowsHostCommandLineEntry> ParseWindowsHostCommandLines(
        ReadOnlyMemory<char> selectedText,
        ReadOnlyMemory<char> shellDelimiter,
        ReadOnlyMemory<char> commandDelimiter)
    {
        ReadOnlySpan<char> textSpan = selectedText.Span;
        ReadOnlySpan<char> shellDelimSpan = shellDelimiter.Span;
        ReadOnlySpan<char> cmdDelimSpan = commandDelimiter.Span;

        var results = new List<WindowsHostCommandLineEntry>();

        foreach (ReadOnlySpan<char> rawLine in textSpan.EnumerateLines())
        {
            ReadOnlySpan<char> line = rawLine.Trim();
            if (line.IsEmpty)
            {
                continue;
            }

            WindowsHostCommandLineEntry? entry = TryParseLine(line, textSpan, shellDelimSpan, cmdDelimSpan);
            if (entry is not null)
            {
                results.Add(entry);
            }
        }

        return results;
    }

    /// <summary>
    /// Parses a single line into a command entry using span-based slicing.
    /// </summary>
    private static WindowsHostCommandLineEntry? TryParseLine(ReadOnlySpan<char> line, ReadOnlySpan<char> selectedText, ReadOnlySpan<char> shellDelimiter, ReadOnlySpan<char> commandDelimiter)
    {
        // Skip known non-command lines (markdown code block fences and shell hints)
        if (ShellHintDetector.HasAnyHint(line) || MarkdownCodeBlockParser.IsFenceLine(line))
        {
            return null;
        }

        // If no command delimiter is present, no working directory can be declared.
        if (line.IndexOf(commandDelimiter) < 0)
        {
            // If no command delimiter is defined (no working directory) but a shell delimiter or shell hint is, then we can still skip the PATH binary check.
            if (line.StartsWith(shellDelimiter) || ShellHintDetector.HasAnyHint(selectedText))
            {
                // Remove shell delimiter from line, if any.
                ReadOnlySpan<char> commandTextWithoutDelimiters = GetTextAfterLastOccurrence(line, shellDelimiter);
                return new(null, commandTextWithoutDelimiters.ToString().AsMemory());
            }

            // Raw line text without a shell delimiter or shell hint may result in non-binary commands being captured (e.g. `ls`).
            // Without knowing which shell to use, only binary files from PATH can be executed.
            ReadOnlySpan<char> fileNameWithOrWithoutExtension = GetFirstWhitespaceToken(line);
            if (IsExecutableBinaryOnEnvironmentPath(fileNameWithOrWithoutExtension, executableExtensions))
            {
                return new(null, line.ToString().AsMemory());
            }

            return null;
        }

        // Assume the working directory may be declared at start of line
        // then check/split it out
        ReadOnlySpan<char> startsAtWorkingPath = line;

        // Any shell indicator MUST be at the start of a selected line, not arbitrarily within it.
        if (line.StartsWith(shellDelimiter))
        {
            startsAtWorkingPath = line[shellDelimiter.Length..].Trim();
        }

        // Slice by the commandDelimiter between the cwd and the actual command
        int cmdDelimIdx = startsAtWorkingPath.IndexOf(commandDelimiter);
        ReadOnlySpan<char> beforeCommandDelimiter = startsAtWorkingPath[..cmdDelimIdx].Trim();
        ReadOnlySpan<char> afterCommandDelimiter = startsAtWorkingPath[(cmdDelimIdx + commandDelimiter.Length)..].Trim();

        if (!line.StartsWith(shellDelimiter) && !ShellHintDetector.HasAnyHint(selectedText))
        {
            // If no shell delimiter is present and no shell hints are provided, only binaries on PATH can be executed
            ReadOnlySpan<char> fileNameWithOrWithoutExtension = GetFirstWhitespaceToken(afterCommandDelimiter);
            if (!IsExecutableBinaryOnEnvironmentPath(fileNameWithOrWithoutExtension, executableExtensions))
            {
                return null;
            }
        }

        // Only the selected commands which don't include a command and/or shell delimiter need to be checked for presence on PATH.
        // With an explicit shell hint or shell delimiter, the selected command may be an interpreted shell function and may pass through.
        return new(beforeCommandDelimiter.ToString().AsMemory(), afterCommandDelimiter.ToString().AsMemory());
    }

    /// <summary>
    /// Checks whether the binary is available on PATH.
    /// </summary>
    private static bool IsExecutableBinaryOnEnvironmentPath(ReadOnlySpan<char> fileNameWithOrWithoutExtension, string[] executableExtensions)
    {
        string? pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathValue))
        {
            return false;
        }

        ReadOnlySpan<char> pathSpan = pathValue.AsSpan();

        foreach (Range range in pathSpan.Split(Path.PathSeparator))
        {
            ReadOnlySpan<char> directory = pathSpan[range];
            if (directory.IsWhiteSpace())
            {
                continue;
            }

            // Check the token as-is (may already include an extension).
            if (File.Exists(Path.Join(directory, fileNameWithOrWithoutExtension)))
            {
                return true;
            }

            // Check with each known executable extension.
            foreach (string ext in executableExtensions)
            {
                if (File.Exists(string.Concat(Path.Join(directory, fileNameWithOrWithoutExtension), ext)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the text after the last occurrence of <paramref name="delimiter"/>, trimmed.
    /// If the delimiter is not found, returns the original text trimmed.
    /// </summary>
    private static ReadOnlySpan<char> GetTextAfterLastOccurrence(ReadOnlySpan<char> text, ReadOnlySpan<char> delimiter)
    {
        int idx = text.LastIndexOf(delimiter);
        return (idx >= 0 ? text[(idx + delimiter.Length)..] : text).Trim();
    }

    /// <summary>
    /// Returns the first whitespace-delimited token from the text.
    /// </summary>
    private static ReadOnlySpan<char> GetFirstWhitespaceToken(ReadOnlySpan<char> text)
    {
        ReadOnlySpan<char> trimmed = text.Trim();
        int idx = trimmed.IndexOfAny(' ', '\t');
        return idx >= 0 ? trimmed[..idx] : trimmed;
    }

    /// <summary>
    /// Represents a single Windows-style command as input into a terminal.
    /// May represent both CMD and PS commands.
    /// </summary>
    private record WindowsHostCommandLineEntry(ReadOnlyMemory<char>? WorkingDirectory, ReadOnlyMemory<char> TerminalCommand);
}
