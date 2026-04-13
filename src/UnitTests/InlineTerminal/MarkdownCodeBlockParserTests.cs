using FluentAssertions;
using WindowSill.InlineTerminal.Core.Parsers;

namespace UnitTests.InlineTerminal;

/// <summary>
/// Unit tests for <see cref="MarkdownCodeBlockParser"/>.
/// </summary>
public class MarkdownCodeBlockParserTests
{
    [Fact]
    internal void TryGetLanguage_WithLanguage_ReturnsTrue()
    {
        bool result = MarkdownCodeBlockParser.TryGetLanguage("```powershell\nGet-Process\n```", out ReadOnlySpan<char> lang);
        result.Should().BeTrue();
        lang.ToString().Should().Be("powershell");
    }

    [Fact]
    internal void TryGetLanguage_WithoutLanguage_ReturnsTrueWithEmpty()
    {
        bool result = MarkdownCodeBlockParser.TryGetLanguage("```\ncode\n```", out ReadOnlySpan<char> lang);
        result.Should().BeTrue();
        lang.ToString().Should().BeEmpty();
    }

    [Fact]
    internal void TryGetLanguage_NoFence_ReturnsFalse()
    {
        bool result = MarkdownCodeBlockParser.TryGetLanguage("just plain text", out _);
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("```bash")]
    [InlineData("  ```python")]
    [InlineData("```")]
    internal void IsFenceLine_FenceLines_ReturnsTrue(string line)
    {
        MarkdownCodeBlockParser.IsFenceLine(line).Should().BeTrue();
    }

    [Theory]
    [InlineData("not a fence")]
    [InlineData("some `` backticks")]
    internal void IsFenceLine_NonFenceLines_ReturnsFalse(string line)
    {
        MarkdownCodeBlockParser.IsFenceLine(line).Should().BeFalse();
    }
}
