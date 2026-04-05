using WindowSill.Terminal.Parsers;

namespace WindowSill.Terminal.Core.Parsers;

/// <summary>
/// Detects shell type hints from markdown code block languages and bash prompt patterns.
/// </summary>
internal static class ShellHintDetector
{
    /// <summary>
    /// Returns whether the text contains a WSL/bash hint via markdown code block language or a bash-style prompt pattern.
    /// </summary>
    internal static bool HasWslHint(ReadOnlySpan<char> selectedText)
    {
        if (MarkdownCodeBlockParser.TryGetLanguage(selectedText, out ReadOnlySpan<char> lang) &&
            (lang.SequenceEqual("wsl") || lang.SequenceEqual("bash") || lang.SequenceEqual("sh") || lang.SequenceEqual("zsh")))
        {
            return true;
        }

        // Also detect bash prompt patterns (user@host:path$)
        return BashPromptParser.ContainsPrompt(selectedText);
    }

    /// <summary>
    /// Returns whether the text contains a Windows PowerShell (5.x) hint.
    /// </summary>
    /// <remarks>
    /// <c>powershell</c> (Windows PowerShell 5, netfx) cannot be disambiguated from <c>pwsh</c> (PowerShell 7+, netx)
    /// using the command prefix (PS) alone.
    /// </remarks>
    internal static bool HasPowerShellHint(ReadOnlySpan<char> selectedText)
    {
        return MarkdownCodeBlockParser.TryGetLanguage(selectedText, out ReadOnlySpan<char> lang)
            && lang.SequenceEqual("powershell");
    }

    /// <summary>
    /// Returns whether the text contains a PowerShell 7+ (pwsh) hint.
    /// </summary>
    internal static bool HasPwshHint(ReadOnlySpan<char> selectedText)
    {
        return MarkdownCodeBlockParser.TryGetLanguage(selectedText, out ReadOnlySpan<char> lang)
            && lang.SequenceEqual("pwsh");
    }

    /// <summary>
    /// Returns whether the text contains a CMD/batch hint.
    /// </summary>
    internal static bool HasCmdHint(ReadOnlySpan<char> selectedText)
    {
        return MarkdownCodeBlockParser.TryGetLanguage(selectedText, out ReadOnlySpan<char> lang)
            && (lang.SequenceEqual("cmd") || lang.SequenceEqual("batch"));
    }

    /// <summary>
    /// Returns whether the text contains any recognized shell hint.
    /// </summary>
    internal static bool HasAnyHint(ReadOnlySpan<char> selectedText)
    {
        return HasCmdHint(selectedText)
            || HasPowerShellHint(selectedText)
            || HasPwshHint(selectedText)
            || HasWslHint(selectedText);
    }
}
