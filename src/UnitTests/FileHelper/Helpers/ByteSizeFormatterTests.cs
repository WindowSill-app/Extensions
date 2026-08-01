using System.Globalization;

using FluentAssertions;

using WindowSill.FileHelper.Helpers;

namespace UnitTests.FileHelper.Helpers;

public class ByteSizeFormatterTests
{
    [Fact]
    internal void Format_UsesBytes_ForValuesUnderOneKilobyte()
    {
        ByteSizeFormatter.Format(0).Should().Be("0 B");
        ByteSizeFormatter.Format(7).Should().Be("7 B");
        ByteSizeFormatter.Format(1023).Should().Be("1023 B");
    }

    [Fact]
    internal void Format_ClampsNegativeInput_ToZeroBytes()
    {
        ByteSizeFormatter.Format(-500).Should().Be("0 B");
    }

    [Fact]
    internal void Format_PromotesToNextUnit_AtEach1024Boundary()
    {
        ByteSizeFormatter.Format(1024).Should().Be("1 KB");
        ByteSizeFormatter.Format(1024L * 1024).Should().Be("1 MB");
        ByteSizeFormatter.Format(1024L * 1024 * 1024).Should().Be("1 GB");
        ByteSizeFormatter.Format(1024L * 1024 * 1024 * 1024).Should().Be("1 TB");
    }

    [Fact]
    internal void Format_ShowsOneDecimalPlace_ForFractionalKilobytesAndUp()
    {
        // 1536 bytes = 1.5 KB.
        string oneAndAHalfKb = 1.5.ToString("0.#", CultureInfo.CurrentCulture) + " KB";
        ByteSizeFormatter.Format(1536).Should().Be(oneAndAHalfKb);
    }

    [Fact]
    internal void Format_MatchesExplorerStyleMegabytes_ForATypicalArchiveSize()
    {
        // 14,881,701 bytes ≈ 14.2 MB (matches what File Explorer reports for the FileHelper .nupkg).
        string expected = (14_881_701d / 1024d / 1024d).ToString("0.#", CultureInfo.CurrentCulture) + " MB";
        ByteSizeFormatter.Format(14_881_701).Should().Be(expected);
        expected.Should().Be("14.2 MB");
    }
}
