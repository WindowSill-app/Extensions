using FluentAssertions;
using WindowSill.InlineTerminal.Core.Commands;

namespace UnitTests.InlineTerminal.Core;

public class AnsiEscapeCodeHelperTests
{
    [Fact]
    public void StripAnsiEscapeCodes_NullInput_ReturnsNull()
    {
        AnsiEscapeCodeHelper.StripAnsiEscapeCodes(null!).Should().BeNull();
    }

    [Fact]
    public void StripAnsiEscapeCodes_EmptyInput_ReturnsEmpty()
    {
        AnsiEscapeCodeHelper.StripAnsiEscapeCodes(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void StripAnsiEscapeCodes_NoEscapeCodes_ReturnsUnchanged()
    {
        string input = "Hello, world!";
        AnsiEscapeCodeHelper.StripAnsiEscapeCodes(input).Should().Be("Hello, world!");
    }

    [Theory]
    [InlineData("\x1B[31mred text\x1B[0m", "red text")]
    [InlineData("\x1B[31;1mGet-ChildItem: \x1B[31;1mCannot find path\x1B[0m", "Get-ChildItem: Cannot find path")]
    [InlineData("\x1B[1;32mSuccess\x1B[0m", "Success")]
    [InlineData("\x1B[0m", "")]
    public void StripAnsiEscapeCodes_CsiColorCodes_StripsCorrectly(string input, string expected)
    {
        AnsiEscapeCodeHelper.StripAnsiEscapeCodes(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("\x1B[1A", "")] // Cursor up
    [InlineData("\x1B[2J", "")] // Clear screen
    [InlineData("\x1B[K", "")]  // Erase line
    [InlineData("\x1B[10;20H", "")] // Cursor position
    public void StripAnsiEscapeCodes_CsiCursorAndEraseSequences_StripsCorrectly(string input, string expected)
    {
        AnsiEscapeCodeHelper.StripAnsiEscapeCodes(input).Should().Be(expected);
    }

    [Fact]
    public void StripAnsiEscapeCodes_OscSequenceWithBel_StripsCorrectly()
    {
        // OSC sequence: ESC ] 0 ; title BEL
        string input = "\x1B]0;Window Title\x07Some text";
        AnsiEscapeCodeHelper.StripAnsiEscapeCodes(input).Should().Be("Some text");
    }

    [Fact]
    public void StripAnsiEscapeCodes_OscSequenceWithST_StripsCorrectly()
    {
        // OSC sequence: ESC ] 0 ; title ESC backslash
        string input = "\x1B]0;Window Title\x1B\\Some text";
        AnsiEscapeCodeHelper.StripAnsiEscapeCodes(input).Should().Be("Some text");
    }

    [Fact]
    public void StripAnsiEscapeCodes_MixedContentAndCodes_PreservesTextOnly()
    {
        string input = "Line1\x1B[32m green \x1B[0mLine2\x1B[1mBold\x1B[0m end";
        AnsiEscapeCodeHelper.StripAnsiEscapeCodes(input).Should().Be("Line1 green Line2Bold end");
    }

    [Fact]
    public void StripAnsiEscapeCodes_PowerShellErrorOutput_StripsCorrectly()
    {
        // Real-world PowerShell error output
        string input = "\x1B[31;1mGet-ChildItem: \x1B[31;1mCannot find path 'E:\\source\\Extension' because it does not exist.\x1B[0m";
        string expected = "Get-ChildItem: Cannot find path 'E:\\source\\Extension' because it does not exist.";
        AnsiEscapeCodeHelper.StripAnsiEscapeCodes(input).Should().Be(expected);
    }

    [Fact]
    public void StripAnsiEscapeCodes_MultipleSgr_StripsAll()
    {
        // Bold + underline + red
        string input = "\x1B[1m\x1B[4m\x1B[31mStyled\x1B[0m";
        AnsiEscapeCodeHelper.StripAnsiEscapeCodes(input).Should().Be("Styled");
    }
}
