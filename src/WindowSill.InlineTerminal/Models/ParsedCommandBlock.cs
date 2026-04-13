namespace WindowSill.InlineTerminal.Models;

/// <summary>
/// Represents a parsed command block with an optional working directory and the full command text.
/// When a prompt is followed by continuation lines (lines without their own prompt), all lines
/// are joined with newlines into a single <see cref="Command"/>.
/// </summary>
/// <param name="WorkingDirectory">The working directory parsed from the prompt, or <see langword="null"/> if none was present.</param>
/// <param name="Command">The full command text, potentially spanning multiple lines separated by newlines.</param>
internal record ParsedCommandBlock(string? WorkingDirectory, string Command);
