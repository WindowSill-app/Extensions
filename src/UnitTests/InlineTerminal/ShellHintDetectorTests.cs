using FluentAssertions;
using WindowSill.InlineTerminal.Core.Parsers;

namespace UnitTests.InlineTerminal;

/// <summary>
/// Unit tests for <see cref="ShellHintDetector"/>.
/// </summary>
public class ShellHintDetectorTests
{
    [Fact]
    internal void HasPowerShellHint_WithPowershellFence_ReturnsTrue()
    {
        ShellHintDetector.HasPowerShellHint("```powershell\nGet-Process\n```").Should().BeTrue();
    }

    [Fact]
    internal void HasPowerShellHint_WithBashFence_ReturnsFalse()
    {
        ShellHintDetector.HasPowerShellHint("```bash\nls\n```").Should().BeFalse();
    }

    [Fact]
    internal void HasPwshHint_WithPwshFence_ReturnsTrue()
    {
        ShellHintDetector.HasPwshHint("```pwsh\nGet-Process\n```").Should().BeTrue();
    }

    [Fact]
    internal void HasCmdHint_WithCmdFence_ReturnsTrue()
    {
        ShellHintDetector.HasCmdHint("```cmd\ndir\n```").Should().BeTrue();
    }

    [Fact]
    internal void HasCmdHint_WithBatchFence_ReturnsTrue()
    {
        ShellHintDetector.HasCmdHint("```batch\necho hello\n```").Should().BeTrue();
    }

    [Theory]
    [InlineData("```wsl")]
    [InlineData("```bash")]
    [InlineData("```sh")]
    [InlineData("```zsh")]
    internal void HasWslHint_WithUnixFences_ReturnsTrue(string fence)
    {
        ShellHintDetector.HasWslHint($"{fence}\nls\n```").Should().BeTrue();
    }

    [Fact]
    internal void HasWslHint_WithBashPrompt_ReturnsTrue()
    {
        ShellHintDetector.HasWslHint("user@host:~/path$ ls -la").Should().BeTrue();
    }

    [Fact]
    internal void HasAnyHint_WithNoHints_ReturnsFalse()
    {
        ShellHintDetector.HasAnyHint("just some text").Should().BeFalse();
    }

    [Theory]
    [InlineData("```powershell")]
    [InlineData("```pwsh")]
    [InlineData("```cmd")]
    [InlineData("```bash")]
    internal void HasAnyHint_WithAnyShellHint_ReturnsTrue(string fence)
    {
        ShellHintDetector.HasAnyHint($"{fence}\ncommand\n```").Should().BeTrue();
    }
}
