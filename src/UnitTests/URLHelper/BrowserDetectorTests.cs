using FluentAssertions;
using WindowSill.URLHelper.Core;

namespace UnitTests.URLHelper;

/// <summary>
/// Unit tests for <see cref="BrowserDetector"/>.
/// </summary>
public class BrowserDetectorTests
{
    [Theory]
    [InlineData("\"C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe\"", "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe")]
    [InlineData("\"C:\\Program Files (x86)\\Mozilla Firefox\\firefox.exe\"", "C:\\Program Files (x86)\\Mozilla Firefox\\firefox.exe")]
    [InlineData("\"C:\\Program Files\\edge\\msedge.exe\" --single-argument %1", "C:\\Program Files\\edge\\msedge.exe")]
    internal void ExtractExePath_QuotedPath_ReturnsUnquotedPath(string commandLine, string expected)
    {
        BrowserDetector.ExtractExePath(commandLine).Should().Be(expected);
    }

    [Theory]
    [InlineData("C:\\chrome.exe", "C:\\chrome.exe")]
    [InlineData("C:\\chrome.exe --flag", "C:\\chrome.exe")]
    internal void ExtractExePath_UnquotedPath_ReturnsPathBeforeArgs(string commandLine, string expected)
    {
        BrowserDetector.ExtractExePath(commandLine).Should().Be(expected);
    }

    [Theory]
    [InlineData("  \"C:\\app.exe\"  ", "C:\\app.exe")]
    [InlineData("  C:\\app.exe  ", "C:\\app.exe")]
    internal void ExtractExePath_WhitespacePadded_TrimsCorrectly(string commandLine, string expected)
    {
        BrowserDetector.ExtractExePath(commandLine).Should().Be(expected);
    }

    [Fact]
    internal void GetInstalledBrowsers_ReturnsNonEmptyList()
    {
        // On any Windows dev machine, at least one browser (Edge) should be present.
        IReadOnlyList<BrowserInfo> browsers = BrowserDetector.GetInstalledBrowsers();
        browsers.Should().NotBeEmpty();
    }

    [Fact]
    internal void GetInstalledBrowsers_NoDuplicateExePaths()
    {
        IReadOnlyList<BrowserInfo> browsers = BrowserDetector.GetInstalledBrowsers();
        browsers.Select(b => b.ExecutablePath.ToLowerInvariant())
            .Should().OnlyHaveUniqueItems();
    }

    [Fact]
    internal void GetInstalledBrowsers_AllExePathsExist()
    {
        IReadOnlyList<BrowserInfo> browsers = BrowserDetector.GetInstalledBrowsers();
        foreach (BrowserInfo browser in browsers)
        {
            File.Exists(browser.ExecutablePath).Should().BeTrue(
                because: $"browser '{browser.Name}' at '{browser.ExecutablePath}' should exist on disk");
        }
    }
}
