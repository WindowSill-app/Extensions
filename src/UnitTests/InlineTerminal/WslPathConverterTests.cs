using FluentAssertions;
using WindowSill.InlineTerminal.Core.Shell;

namespace UnitTests.InlineTerminal;

/// <summary>
/// Unit tests for <see cref="WslPathConverter"/>.
/// </summary>
public class WslPathConverterTests
{
    [Theory]
    [InlineData(@"C:\Users\me", "/mnt/c/Users/me")]
    [InlineData(@"D:\Projects\app", "/mnt/d/Projects/app")]
    [InlineData(@"E:\source\code\file.txt", "/mnt/e/source/code/file.txt")]
    internal void ConvertToWslPath_WindowsDrivePath_ConvertsCorrectly(string input, string expected)
    {
        WslPathConverter.ConvertToWslPath(input).Should().Be(expected);
    }

    [Fact]
    internal void ConvertToWslPath_UnixPath_ReturnsUnchanged()
    {
        WslPathConverter.ConvertToWslPath("/home/user/file").Should().Be("/home/user/file");
    }

    [Fact]
    internal void ConvertToWslPath_LowercaseDrive_LowersCorrectly()
    {
        WslPathConverter.ConvertToWslPath(@"c:\temp").Should().Be("/mnt/c/temp");
    }

    [Fact]
    internal void ConvertToWslPath_UppercaseDrive_LowersLetter()
    {
        WslPathConverter.ConvertToWslPath(@"Z:\data").Should().Be("/mnt/z/data");
    }

    [Fact]
    internal void ConvertToWslPath_ForwardSlashWindowsPath_ConvertsCorrectly()
    {
        WslPathConverter.ConvertToWslPath("C:/Users/me").Should().Be("/mnt/c/Users/me");
    }

    [Fact]
    internal void ConvertToWslPath_RelativePath_ReplacesBackslashes()
    {
        WslPathConverter.ConvertToWslPath(@"some\relative\path").Should().Be("some/relative/path");
    }
}
