using System.Text.RegularExpressions;

namespace WindowSill.InlineTerminal.Core.Parsers;

/// <summary>
/// Parses bash-style prompts (<c>user@host:path$ command</c>) from selected text.
/// </summary>
internal static partial class BashPromptParser
{
    /// <summary>
    /// Returns whether the selected text contains at least one bash-style prompt pattern.
    /// </summary>
    internal static bool ContainsPrompt(ReadOnlySpan<char> selectedText)
    {
        foreach (ReadOnlySpan<char> line in selectedText.EnumerateLines())
        {
            ReadOnlySpan<char> trimmed = line.Trim();
            if (!trimmed.IsEmpty && BashPromptRegex().IsMatch(trimmed))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Parses and returns the first terminal command from a bash-style prompt.
    /// </summary>
    internal static ReadOnlyMemory<char>? GetTerminalCommand(ReadOnlySpan<char> selectedText)
    {
        Match match = GetFirstMatch(selectedText);
        if (!match.Success)
        {
            return null;
        }

        ReadOnlySpan<char> command = match.Groups["command"].ValueSpan.Trim();
        if (command.IsEmpty)
        {
            return null;
        }

        return command.ToString().AsMemory();
    }

    /// <summary>
    /// Parses and returns the first working directory from a bash-style prompt.
    /// </summary>
    internal static ReadOnlyMemory<char>? GetWorkingDirectory(ReadOnlySpan<char> selectedText)
    {
        Match match = GetFirstMatch(selectedText);
        if (!match.Success)
        {
            return null;
        }

        ReadOnlySpan<char> path = match.Groups["path"].ValueSpan.Trim();
        if (path.IsEmpty)
        {
            return (ReadOnlyMemory<char>)null;
        }

        return path.ToString().AsMemory();
    }

    /// <summary>
    /// Parses and returns the hostname from the first bash-style prompt.
    /// </summary>
    internal static ReadOnlyMemory<char>? GetMachineName(ReadOnlySpan<char> selectedText)
    {
        Match match = GetFirstMatch(selectedText);
        if (!match.Success)
        {
            return null;
        }

        ReadOnlySpan<char> host = match.Groups["host"].ValueSpan.Trim();
        if (host.IsEmpty)
        {
            return (ReadOnlyMemory<char>)null;
        }

        return host.ToString().AsMemory();
    }

    /// <summary>
    /// Parses and returns the username from the first bash-style prompt.
    /// </summary>
    internal static ReadOnlyMemory<char>? GetUserName(ReadOnlySpan<char> selectedText)
    {
        Match match = GetFirstMatch(selectedText);
        if (!match.Success)
        {
            return null;
        }

        return match.Groups["user"].ValueSpan.ToString().AsMemory();
    }

    /// <summary>
    /// Returns the first regex match for a bash-style prompt across all lines.
    /// Only allocates a string for the matching line.
    /// </summary>
    private static Match GetFirstMatch(ReadOnlySpan<char> selectedText)
    {
        foreach (ReadOnlySpan<char> line in selectedText.EnumerateLines())
        {
            ReadOnlySpan<char> trimmed = line.Trim();
            if (trimmed.IsEmpty)
            {
                continue;
            }

            // Only allocate a string when the span-based check confirms a likely match.
            if (BashPromptRegex().IsMatch(trimmed))
            {
                return BashPromptRegex().Match(trimmed.ToString());
            }
        }

        return Match.Empty;
    }

    /// <summary>
    /// Matches a bash-style prompt: <c>user@host:path$ command</c>.
    /// Captures: user, host, path (working directory), and command.
    /// </summary>
    [GeneratedRegex(@"^(?<user>[a-zA-Z0-9._-]+)@(?<host>[a-zA-Z0-9._-]+):(?<path>[^\$]+?)\$\s*(?<command>.+)?$")]
    private static partial Regex BashPromptRegex();
}
