using FluentAssertions;
using WindowSill.URLHelper.Core;

namespace UnitTests.URLHelper;

/// <summary>
/// Unit tests for <see cref="BrowserMatcher"/>.
/// </summary>
public class BrowserMatcherTests
{
    [Theory]
    [InlineData("msedge.exe", true)]
    [InlineData("chrome.exe", true)]
    [InlineData("firefox.exe", true)]
    [InlineData("brave.exe", true)]
    [InlineData("opera.exe", true)]
    [InlineData("vivaldi.exe", true)]
    [InlineData("zen.exe", true)]
    [InlineData("arc.exe", true)]
    [InlineData("waterfox.exe", true)]
    [InlineData("chromium.exe", true)]
    internal void IsKnownBrowser_KnownBrowserExeNames_ReturnsTrue(string appId, bool expected)
    {
        BrowserMatcher.IsKnownBrowser(appId).Should().Be(expected);
    }

    [Theory]
    [InlineData("notepad.exe")]
    [InlineData("code.exe")]
    [InlineData("devenv.exe")]
    [InlineData("explorer.exe")]
    internal void IsKnownBrowser_NonBrowserApps_ReturnsFalse(string appId)
    {
        BrowserMatcher.IsKnownBrowser(appId).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    internal void IsKnownBrowser_NullOrEmpty_ReturnsFalse(string? appId)
    {
        BrowserMatcher.IsKnownBrowser(appId).Should().BeFalse();
    }

    [Fact]
    internal void IsKnownBrowser_FullPathWithBrowserExe_ReturnsTrue()
    {
        BrowserMatcher.IsKnownBrowser(@"C:\Program Files\Google\Chrome\Application\chrome.exe")
            .Should().BeTrue();
    }

    [Fact]
    internal void IsKnownBrowser_CaseInsensitive_ReturnsTrue()
    {
        BrowserMatcher.IsKnownBrowser("MSEDGE.EXE").Should().BeTrue();
        BrowserMatcher.IsKnownBrowser("Chrome.Exe").Should().BeTrue();
    }

    [Theory]
    [InlineData(@"C:\Program Files\Google\Chrome\Application\chrome.exe", "chrome.exe", true)]
    [InlineData(@"C:\Program Files\Google\Chrome\Application\chrome.exe", @"C:\Program Files\Google\Chrome\Application\chrome.exe", true)]
    [InlineData(@"C:\Program Files\Google\Chrome\Application\chrome.exe", "msedge.exe", false)]
    [InlineData(@"C:\Program Files\Google\Chrome\Application\chrome.exe", "firefox.exe", false)]
    internal void IsMatchingBrowser_MatchesByExeName(string browserExePath, string appIdentifier, bool expected)
    {
        BrowserMatcher.IsMatchingBrowser(browserExePath, appIdentifier).Should().Be(expected);
    }

    [Fact]
    internal void IsMatchingBrowser_CaseInsensitive()
    {
        BrowserMatcher.IsMatchingBrowser(
            @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
            "MSEDGE.EXE")
            .Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    internal void IsMatchingBrowser_NullOrEmptyAppId_ReturnsFalse(string? appId)
    {
        BrowserMatcher.IsMatchingBrowser(@"C:\chrome.exe", appId).Should().BeFalse();
    }

    [Theory]
    [InlineData("chrome.exe", "--incognito", "Incognito")]
    [InlineData("brave.exe", "--incognito", "Incognito")]
    [InlineData("vivaldi.exe", "--incognito", "Incognito")]
    [InlineData("chromium.exe", "--incognito", "Incognito")]
    [InlineData("arc.exe", "--incognito", "Incognito")]
    [InlineData("msedge.exe", "--inprivate", "InPrivate")]
    [InlineData("firefox.exe", "-private-window", "Private Window")]
    [InlineData("waterfox.exe", "-private-window", "Private Window")]
    [InlineData("zen.exe", "-private-window", "Private Window")]
    [InlineData("opera.exe", "--private", "Private Window")]
    internal void GetPrivateModeInfo_KnownBrowsers_ReturnsCorrectFlagAndName(
        string exeName, string expectedFlag, string expectedName)
    {
        (string? flag, string? name) = BrowserMatcher.GetPrivateModeInfo(@$"C:\Browsers\{exeName}");
        flag.Should().Be(expectedFlag);
        name.Should().Be(expectedName);
    }

    [Theory]
    [InlineData(@"C:\Program Files\Google\Chrome\Application\chrome.exe", "--incognito")]
    [InlineData(@"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe", "--inprivate")]
    internal void GetPrivateModeInfo_FullPaths_ReturnsCorrectFlag(string exePath, string expectedFlag)
    {
        (string? flag, _) = BrowserMatcher.GetPrivateModeInfo(exePath);
        flag.Should().Be(expectedFlag);
    }

    [Fact]
    internal void GetPrivateModeInfo_UnknownBrowser_ReturnsNulls()
    {
        (string? flag, string? name) = BrowserMatcher.GetPrivateModeInfo(@"C:\unknown\mybrowser.exe");
        flag.Should().BeNull();
        name.Should().BeNull();
    }

    [Fact]
    internal void GetPrivateModeInfo_CaseInsensitive()
    {
        (string? flag, _) = BrowserMatcher.GetPrivateModeInfo(@"C:\CHROME.EXE");
        flag.Should().Be("--incognito");
    }
}
