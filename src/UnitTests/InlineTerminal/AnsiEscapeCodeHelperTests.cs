using FluentAssertions;
using WindowSill.InlineTerminal.Core;

namespace UnitTests.InlineTerminal;

/// <summary>
/// Unit tests for <see cref="AnsiEscapeCodeHelper"/>.
/// </summary>
public class AnsiEscapeCodeHelperTests
{
    [Fact]
    internal void StripAnsiEscapeCodes_NullInput_ReturnsNull()
    {
        AnsiEscapeCodeHelper.StripAnsiEscapeCodes(null!).Should().BeNull();
    }

    [Fact]
    internal void StripAnsiEscapeCodes_EmptyInput_ReturnsEmpty()
    {
        AnsiEscapeCodeHelper.StripAnsiEscapeCodes(string.Empty).Should().BeEmpty();
    }

    [Fact]
    internal void StripAnsiEscapeCodes_NoEscapeCodes_ReturnsOriginal()
    {
        string input = "Hello, World!";
        AnsiEscapeCodeHelper.StripAnsiEscapeCodes(input).Should().Be("Hello, World!");
    }

    [Fact]
    internal void StripAnsiEscapeCodes_CsiSequence_IsRemoved()
    {
        // Bold text: ESC[1m
        string input = "\x1B[1mBold Text\x1B[0m";
        AnsiEscapeCodeHelper.StripAnsiEscapeCodes(input).Should().Be("Bold Text");
    }

    [Fact]
    internal void StripAnsiEscapeCodes_ColorCodes_AreRemoved()
    {
        // Red foreground: ESC[31m, reset: ESC[0m
        string input = "\x1B[31mError\x1B[0m: something failed";
        AnsiEscapeCodeHelper.StripAnsiEscapeCodes(input).Should().Be("Error: something failed");
    }

    [Fact]
    internal void StripAnsiEscapeCodes_OscSequence_IsRemoved()
    {
        // OSC title: ESC]0;Title BEL
        string input = "\x1B]0;Window Title\x07Some output";
        AnsiEscapeCodeHelper.StripAnsiEscapeCodes(input).Should().Be("Some output");
    }

    [Fact]
    internal void StripAnsiEscapeCodes_MultipleSequences_AllRemoved()
    {
        string input = "\x1B[32m[OK]\x1B[0m \x1B[1mDone\x1B[0m";
        AnsiEscapeCodeHelper.StripAnsiEscapeCodes(input).Should().Be("[OK] Done");
    }

    [Fact]
    internal void StripAnsiEscapeCodes_CursorMovement_IsRemoved()
    {
        // Cursor up: ESC[A
        string input = "\x1B[ALine above";
        AnsiEscapeCodeHelper.StripAnsiEscapeCodes(input).Should().Be("Line above");
    }
}
