using FluentAssertions;
using WindowSill.Terminal.Core.Shell;

namespace UnitTests.Terminal.Core;

public class WslPathConverterTests
{
    [Theory]
    [InlineData(@"C:\Users\me\project", "/mnt/c/Users/me/project")]
    [InlineData(@"D:\Data\files", "/mnt/d/Data/files")]
    [InlineData(@"c:\lower", "/mnt/c/lower")]
    [InlineData(@"Z:\", "/mnt/z/")]
    public void ConvertToWslPath_WindowsDrivePath_ConvertsCorrectly(string input, string expected)
    {
        // Act
        string result = WslPathConverter.ConvertToWslPath(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("/home/user/project")]
    [InlineData("/mnt/c/existing")]
    [InlineData("/tmp")]
    public void ConvertToWslPath_UnixPath_ReturnsUnchanged(string input)
    {
        // Act
        string result = WslPathConverter.ConvertToWslPath(input);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void ConvertToWslPath_ForwardSlashDrivePath_ConvertsCorrectly()
    {
        // Act
        string result = WslPathConverter.ConvertToWslPath("C:/Users/me");

        // Assert
        result.Should().Be("/mnt/c/Users/me");
    }

    [Fact]
    public void ConvertToWslPath_RelativePath_ReplacesBackslashes()
    {
        // Act
        string result = WslPathConverter.ConvertToWslPath(@"some\relative\path");

        // Assert
        result.Should().Be("some/relative/path");
    }

    [Fact]
    public void ConvertToWslPath_DriveLetterIsCaseInsensitive()
    {
        // Act & Assert
        WslPathConverter.ConvertToWslPath(@"C:\test").Should().Be("/mnt/c/test");
        WslPathConverter.ConvertToWslPath(@"c:\test").Should().Be("/mnt/c/test");
    }
}
