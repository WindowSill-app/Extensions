using FluentAssertions;
using WindowSill.InlineTerminal.Services;

namespace UnitTests.InlineTerminal;

/// <summary>
/// Unit tests for <see cref="SecurityService"/>.
/// </summary>
public class SecurityServiceTests
{
    [Theory]
    [InlineData("msedge.exe", true)]
    [InlineData("chrome.exe", true)]
    [InlineData("firefox.exe", true)]
    [InlineData("brave.exe", true)]
    [InlineData("opera.exe", true)]
    [InlineData("vivaldi.exe", true)]
    [InlineData("zen.exe", true)]
    internal void IsBrowserApplication_KnownBrowsers_ReturnsTrue(string appId, bool expected)
    {
        SecurityService.IsBrowserApplication(appId).Should().Be(expected);
    }

    [Theory]
    [InlineData("notepad.exe")]
    [InlineData("code.exe")]
    [InlineData("powershell.exe")]
    [InlineData("devenv.exe")]
    internal void IsBrowserApplication_NonBrowserApps_ReturnsFalse(string appId)
    {
        SecurityService.IsBrowserApplication(appId).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    internal void IsBrowserApplication_NullOrEmpty_ReturnsFalse(string? appId)
    {
        SecurityService.IsBrowserApplication(appId).Should().BeFalse();
    }

    [Fact]
    internal void IsBrowserApplication_PathWithBrowserExe_ReturnsTrue()
    {
        // ApplicationIdentifier may contain a full path
        SecurityService.IsBrowserApplication(@"C:\Program Files\Google\Chrome\Application\chrome.exe")
            .Should().BeTrue();
    }

    [Fact]
    internal void IsBrowserApplication_CaseInsensitive_ReturnsTrue()
    {
        SecurityService.IsBrowserApplication("MSEDGE.EXE").Should().BeTrue();
        SecurityService.IsBrowserApplication("Chrome.Exe").Should().BeTrue();
    }
}
