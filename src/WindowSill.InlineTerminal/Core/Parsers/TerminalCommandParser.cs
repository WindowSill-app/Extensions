using System.Text;
using WindowSill.InlineTerminal.Models;
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
    /// Parses the selected text into one or more command blocks. A new block starts each time a
    /// shell prompt (e.g. <c>PS C:\path&gt;</c>) is encountered. Lines that follow a prompt without
    /// introducing a new prompt are appended to the current block so the entire script can be
    /// executed in one shell invocation.
    /// </summary>
    internal static List<ParsedCommandBlock> GetCommandBlocks(string selectedText)
    {
        List<WindowsHostCommandLineEntry> entries
            = ParseWindowsHostCommandLines(
                selectedText.AsMemory(),
                shellDelimiter: "PS".AsMemory(),
                commandDelimiter: ">".AsMemory());

        if (entries.Count > 0)
        {
            return entries.Select(e => new ParsedCommandBlock(
                e.WorkingDirectory?.ToString(),
                e.TerminalCommand.ToString())).ToList();
        }

        // Fall back to bash prompt parsing.
        string? bashCommand = BashPromptParser.GetTerminalCommand(selectedText)?.ToString();
        if (bashCommand is not null)
        {
            string? bashWorkingDir = BashPromptParser.GetWorkingDirectory(selectedText)?.ToString();
            return [new ParsedCommandBlock(bashWorkingDir, bashCommand)];
        }

        return [];
    }

    /// <summary>
    /// Given the full text selected by the user, parses and returns the first inferred working directory.
    /// </summary>
    internal static string? GetFirstWorkingDirectory(string selectedText)
    {
        return GetCommandBlocks(selectedText).FirstOrDefault()?.WorkingDirectory;
    }

    /// <summary>
    /// Given the full text selected by the user, parses and returns the first inferred terminal command from the text.
    /// </summary>
    internal static string? GetFirstTerminalCommand(string selectedText)
    {
        return GetCommandBlocks(selectedText).FirstOrDefault()?.Command;
    }

    /// <summary>
    /// Parses the selected text and separates out individual Windows host shell invocations.
    /// Continuation lines (lines without a prompt) are grouped into the preceding prompt's block.
    /// </summary>
    private static List<WindowsHostCommandLineEntry> ParseWindowsHostCommandLines(
        ReadOnlyMemory<char> selectedText,
        ReadOnlyMemory<char> shellDelimiter,
        ReadOnlyMemory<char> commandDelimiter)
    {
        ReadOnlySpan<char> textSpan = selectedText.Span;
        ReadOnlySpan<char> shellDelimSpan = shellDelimiter.Span;
        ReadOnlySpan<char> cmdDelimSpan = commandDelimiter.Span;

        var results = new List<WindowsHostCommandLineEntry>();
        WindowsHostCommandLineEntry? currentBlock = null;
        StringBuilder? currentCommandBuilder = null;

        foreach (ReadOnlySpan<char> rawLine in textSpan.EnumerateLines())
        {
            ReadOnlySpan<char> line = rawLine.Trim();
            if (line.IsEmpty)
            {
                continue;
            }

            // Skip markdown fences and shell hint lines.
            if (ShellHintDetector.HasAnyHint(line) || MarkdownCodeBlockParser.IsFenceLine(line))
            {
                continue;
            }

            // Check whether this line starts a new prompt-based entry.
            WindowsHostCommandLineEntry? promptEntry = TryParsePromptLine(line, textSpan, shellDelimSpan, cmdDelimSpan);
            if (promptEntry is not null)
            {
                // Flush the previous block.
                FlushBlock(results, ref currentBlock, ref currentCommandBuilder);

                currentBlock = promptEntry;
                currentCommandBuilder = new StringBuilder(promptEntry.TerminalCommand.ToString());
                continue;
            }

            // Not a prompt line – append as a continuation if a block is open.
            if (currentBlock is not null)
            {
                currentCommandBuilder!.Append('\n').Append(line);
                continue;
            }

            // No open block. Try to start a standalone (non-prompt) entry.
            WindowsHostCommandLineEntry? standaloneEntry = TryParseStandaloneLine(line, textSpan, shellDelimSpan);
            if (standaloneEntry is not null)
            {
                currentBlock = standaloneEntry;
                currentCommandBuilder = new StringBuilder(standaloneEntry.TerminalCommand.ToString());
            }
        }

        FlushBlock(results, ref currentBlock, ref currentCommandBuilder);
        return results;
    }

    /// <summary>
    /// Commits the current block (if any) into the results list, replacing its command text
    /// with the accumulated multi-line builder content.
    /// </summary>
    private static void FlushBlock(
        List<WindowsHostCommandLineEntry> results,
        ref WindowsHostCommandLineEntry? currentBlock,
        ref StringBuilder? commandBuilder)
    {
        if (currentBlock is null)
        {
            return;
        }

        results.Add(currentBlock with { TerminalCommand = commandBuilder!.ToString().AsMemory() });
        currentBlock = null;
        commandBuilder = null;
    }

    /// <summary>
    /// Attempts to parse a line that starts with a shell prompt (e.g. <c>PS C:\path&gt; command</c>).
    /// Returns <see langword="null"/> if the line is not a prompt line.
    /// </summary>
    private static WindowsHostCommandLineEntry? TryParsePromptLine(
        ReadOnlySpan<char> line,
        ReadOnlySpan<char> selectedText,
        ReadOnlySpan<char> shellDelimiter,
        ReadOnlySpan<char> commandDelimiter)
    {
        // A prompt line must contain the command delimiter (e.g. '>') to declare a working directory,
        // OR start with the shell delimiter (e.g. 'PS') to be a PS-without-cwd prompt.
        bool startsWithShell = line.StartsWith(shellDelimiter);
        bool hasCommandDelimiter = line.IndexOf(commandDelimiter) >= 0;

        if (!startsWithShell)
        {
            return null;
        }

        if (!hasCommandDelimiter)
        {
            // PS without working directory: "PS dotnet build"
            ReadOnlySpan<char> commandTextWithoutDelimiters = GetTextAfterLastOccurrence(line, shellDelimiter);
            if (commandTextWithoutDelimiters.IsEmpty)
            {
                return null;
            }

            return new(null, commandTextWithoutDelimiters.ToString().AsMemory());
        }

        // PS with working directory: "PS C:\code> dotnet build"
        ReadOnlySpan<char> startsAtWorkingPath = line[shellDelimiter.Length..].Trim();
        int cmdDelimIdx = startsAtWorkingPath.IndexOf(commandDelimiter);
        ReadOnlySpan<char> workingDir = startsAtWorkingPath[..cmdDelimIdx].Trim();
        ReadOnlySpan<char> command = startsAtWorkingPath[(cmdDelimIdx + commandDelimiter.Length)..].Trim();

        return new(workingDir.ToString().AsMemory(), command.ToString().AsMemory());
    }

    /// <summary>
    /// Attempts to parse a non-prompt line as a standalone command. The line must either be a
    /// recognized binary on PATH or occur inside a text block that carries a shell hint.
    /// Returns <see langword="null"/> if the line cannot be identified as a command.
    /// </summary>
    private static WindowsHostCommandLineEntry? TryParseStandaloneLine(
        ReadOnlySpan<char> line,
        ReadOnlySpan<char> selectedText,
        ReadOnlySpan<char> shellDelimiter)
    {
        if (ShellHintDetector.HasAnyHint(selectedText))
        {
            return new(null, line.ToString().AsMemory());
        }

        // Without a shell hint, only known binaries on PATH are accepted.
        ReadOnlySpan<char> fileNameWithOrWithoutExtension = GetFirstWhitespaceToken(line);
        if (IsExecutableBinaryOnEnvironmentPath(fileNameWithOrWithoutExtension, executableExtensions))
        {
            return new(null, line.ToString().AsMemory());
        }

        return null;
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
