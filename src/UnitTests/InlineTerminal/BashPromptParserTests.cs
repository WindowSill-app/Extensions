using FluentAssertions;
using WindowSill.InlineTerminal.Core.Parsers;

namespace UnitTests.InlineTerminal;

/// <summary>
/// Unit tests for <see cref="BashPromptParser"/>.
/// </summary>
public class BashPromptParserTests
{
    [Fact]
    internal void ContainsPrompt_ValidBashPrompt_ReturnsTrue()
    {
        BashPromptParser.ContainsPrompt("user@host:~/path$ ls -la").Should().BeTrue();
    }

    [Fact]
    internal void ContainsPrompt_NoBashPrompt_ReturnsFalse()
    {
        BashPromptParser.ContainsPrompt("just some text").Should().BeFalse();
    }

    [Fact]
    internal void GetTerminalCommand_ValidPrompt_ReturnsCommand()
    {
        ReadOnlyMemory<char>? command = BashPromptParser.GetTerminalCommand("user@host:~/path$ ls -la");
        command.Should().NotBeNull();
        command!.Value.ToString().Should().Be("ls -la");
    }

    [Fact]
    internal void GetTerminalCommand_NoCommand_ReturnsNull()
    {
        BashPromptParser.GetTerminalCommand("not a prompt").Should().BeNull();
    }

    [Fact]
    internal void GetWorkingDirectory_ValidPrompt_ReturnsPath()
    {
        ReadOnlyMemory<char>? wd = BashPromptParser.GetWorkingDirectory("user@host:~/projects$ make");
        wd.Should().NotBeNull();
        wd!.Value.ToString().Should().Be("~/projects");
    }

    [Fact]
    internal void GetMachineName_ValidPrompt_ReturnsHost()
    {
        ReadOnlyMemory<char>? host = BashPromptParser.GetMachineName("john@myserver:~$ whoami");
        host.Should().NotBeNull();
        host!.Value.ToString().Should().Be("myserver");
    }

    [Fact]
    internal void GetUserName_ValidPrompt_ReturnsUser()
    {
        ReadOnlyMemory<char>? user = BashPromptParser.GetUserName("john@myserver:~$ whoami");
        user.Should().NotBeNull();
        user!.Value.ToString().Should().Be("john");
    }

    [Fact]
    internal void ContainsPrompt_MultilineWithPrompt_ReturnsTrue()
    {
        string text = "some output\nuser@host:~/dir$ echo hello\nmore output";
        BashPromptParser.ContainsPrompt(text).Should().BeTrue();
    }
}
