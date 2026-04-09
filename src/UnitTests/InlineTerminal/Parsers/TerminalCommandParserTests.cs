using FluentAssertions;
using WindowSill.InlineTerminal.Core.Parsers;
using WindowSill.InlineTerminal.Parsers;

namespace UnitTests.InlineTerminal.Parsers;

public class ShellHintDetectorTests
{
    [Theory]
    [InlineData("```powershell\nGet-Process\n```")]
    [InlineData("```bash\nls -la\n```")]
    [InlineData("```cmd\ndir\n```")]
    [InlineData("```pwsh\nGet-Process\n```")]
    [InlineData("```wsl\nls\n```")]
    [InlineData("```sh\nls\n```")]
    [InlineData("```zsh\nls\n```")]
    [InlineData("```batch\ndir\n```")]
    public void HasHint_ShellLanguages_DetectedByAtLeastOneHintMethod(string input)
    {
        bool detected = ShellHintDetector.HasCmdHint(input)
            || ShellHintDetector.HasPowerShellHint(input)
            || ShellHintDetector.HasPwshHint(input)
            || ShellHintDetector.HasWslHint(input);
        detected.Should().BeTrue();
    }

    [Theory]
    [InlineData("```\nsome code\n```")]
    [InlineData("no code block here")]
    [InlineData("```python\nprint('hi')\n```")]
    [InlineData("no hints")]
    public void HasHint_NonShellLanguages_NotDetected(string input)
    {
        bool detected = ShellHintDetector.HasCmdHint(input)
            || ShellHintDetector.HasPowerShellHint(input)
            || ShellHintDetector.HasPwshHint(input)
            || ShellHintDetector.HasWslHint(input);
        detected.Should().BeFalse();
    }

    [Fact]
    public void HasWslHint_InsideBlockquote_StillDetected()
    {
        string input = "> ```bash\n> ls\n> ```";
        ShellHintDetector.HasWslHint(input).Should().BeTrue();
    }

    [Fact]
    public void HasCmdHint_ReturnsTrueForCmd()
    {
        ShellHintDetector.HasCmdHint("```cmd\ndir\n```").Should().BeTrue();
    }

    [Fact]
    public void HasCmdHint_ReturnsTrueForBatch()
    {
        ShellHintDetector.HasCmdHint("```batch\ndir\n```").Should().BeTrue();
    }

    [Fact]
    public void HasPowerShellHint_ReturnsTrueForPowershell()
    {
        ShellHintDetector.HasPowerShellHint("```powershell\nGet-Process\n```").Should().BeTrue();
    }

    [Fact]
    public void HasPwshHint_ReturnsTrueForPwsh()
    {
        ShellHintDetector.HasPwshHint("```pwsh\nGet-Process\n```").Should().BeTrue();
    }

    [Fact]
    public void HasWslHint_ReturnsTrueForBashPrompt()
    {
        ShellHintDetector.HasWslHint("user@host:~/projects$ ls -la")
            .Should().BeTrue();
    }

    [Theory]
    [InlineData("```sh\nls\n```")]
    [InlineData("```zsh\nls\n```")]
    public void HasWslHint_ReturnsTrueForShAndZsh(string input)
    {
        ShellHintDetector.HasWslHint(input).Should().BeTrue();
    }
}

public class BashPromptParserTests
{
    [Theory]
    [InlineData("user@host:~/projects$ ls -la", "ls -la")]
    [InlineData("root@server:/var/log$ tail -f syslog", "tail -f syslog")]
    [InlineData("dev@machine:~$ git status", "git status")]
    public void GetTerminalCommand_ExtractsCommand(string input, string expected)
    {
        BashPromptParser.GetTerminalCommand(input)?.ToString()
            .Should().Be(expected);
    }

    [Fact]
    public void GetTerminalCommand_NoPrompt_ReturnsNull()
    {
        BashPromptParser.GetTerminalCommand("just some text")
            .HasValue.Should().BeFalse();
    }

    [Fact]
    public void GetTerminalCommand_PromptWithoutCommand_ReturnsNull()
    {
        BashPromptParser.GetTerminalCommand("user@host:~$ ")
            .HasValue.Should().BeFalse();
    }

    [Theory]
    [InlineData("user@host:~/projects$ ls", "~/projects")]
    [InlineData("root@server:/var/log$ tail", "/var/log")]
    [InlineData("dev@machine:~$ git status", "~")]
    public void GetWorkingDirectory_ExtractsPath(string input, string expected)
    {
        BashPromptParser.GetWorkingDirectory(input)?.ToString()
            .Should().Be(expected);
    }

    [Fact]
    public void GetWorkingDirectory_NoPrompt_ReturnsNull()
    {
        BashPromptParser.GetWorkingDirectory("no prompt here")
            .HasValue.Should().BeFalse();
    }

    [Theory]
    [InlineData("user@myhost:~$ ls", "myhost")]
    [InlineData("root@server.local:/tmp$ cd", "server.local")]
    public void GetMachineName_ExtractsHost(string input, string expected)
    {
        BashPromptParser.GetMachineName(input)?.ToString()
            .Should().Be(expected);
    }

    [Fact]
    public void GetMachineName_NoPrompt_ReturnsNull()
    {
        BashPromptParser.GetMachineName("no prompt")
            .HasValue.Should().BeFalse();
    }

    [Theory]
    [InlineData("alice@host:~$ ls", "alice")]
    [InlineData("root@server:/tmp$ cd", "root")]
    public void GetUserName_ExtractsUser(string input, string expected)
    {
        BashPromptParser.GetUserName(input)?.ToString()
            .Should().Be(expected);
    }

    [Fact]
    public void GetUserName_NoPrompt_ReturnsNull()
    {
        BashPromptParser.GetUserName("no prompt")
            .HasValue.Should().BeFalse();
    }

    [Fact]
    public void BashPromptParsing_MultipleLines_UsesFirstPrompt()
    {
        string input = "some output line\nuser@host:~/src$ make build\nroot@other:/tmp$ ls";
        BashPromptParser.GetTerminalCommand(input)?.ToString().Should().Be("make build");
        BashPromptParser.GetUserName(input)?.ToString().Should().Be("user");
        BashPromptParser.GetMachineName(input)?.ToString().Should().Be("host");
        BashPromptParser.GetWorkingDirectory(input)?.ToString().Should().Be("~/src");
    }
}

public class TerminalCommandParserTests
{
    [Fact]
    public void GetFirstTerminalCommand_PsPrompt_ExtractsCommand()
    {
        TerminalCommandParser.GetFirstTerminalCommand("PS C:\\Users\\dev> Get-Process")
            .Should().Be("Get-Process");
    }

    [Fact]
    public void GetFirstTerminalCommand_BashFallback_ExtractsCommand()
    {
        TerminalCommandParser.GetFirstTerminalCommand("user@host:~/src$ make build")
            .Should().Be("make build");
    }

    [Fact]
    public void GetFirstWorkingDirectory_PsPrompt_ExtractsPath()
    {
        TerminalCommandParser.GetFirstWorkingDirectory("PS C:\\Users\\dev> dir")
            .Should().Be("C:\\Users\\dev");
    }

    [Fact]
    public void GetFirstWorkingDirectory_BashFallback_ExtractsPath()
    {
        TerminalCommandParser.GetFirstWorkingDirectory("user@host:~/src$ ls")
            .Should().Be("~/src");
    }

    [Fact]
    public void GetFirstWorkingDirectory_PsWithWorkingDir_ParsesCorrectly()
    {
        TerminalCommandParser.GetFirstWorkingDirectory("PS C:\\code> dotnet build")
            .Should().Be("C:\\code");
    }

    [Fact]
    public void GetFirstTerminalCommand_PsWithWorkingDir_ParsesCorrectly()
    {
        TerminalCommandParser.GetFirstTerminalCommand("PS C:\\code> dotnet build")
            .Should().Be("dotnet build");
    }

    [Fact]
    public void GetFirstTerminalCommand_PsWithoutWorkingDir_ParsesCommand()
    {
        TerminalCommandParser.GetFirstTerminalCommand("PS dotnet build")
            .Should().Be("dotnet build");
    }

    [Fact]
    public void GetFirstWorkingDirectory_PsWithoutWorkingDir_ReturnsNull()
    {
        TerminalCommandParser.GetFirstWorkingDirectory("PS dotnet build")
            .Should().BeNull();
    }

    [Fact]
    public void GetFirstTerminalCommand_MultiplePrompts_ReturnsFirst()
    {
        TerminalCommandParser.GetFirstTerminalCommand("PS C:\\code> dotnet build\nPS C:\\code> dotnet test")
            .Should().Be("dotnet build");
    }

    [Fact]
    public void GetFirstTerminalCommand_SkipsMarkdownHints()
    {
        TerminalCommandParser.GetFirstTerminalCommand("```powershell\nPS C:\\code> Get-Process\n```")
            .Should().Be("Get-Process");
    }

    [Fact]
    public void GetFirstTerminalCommand_KnownBinaryWithoutShellHint_ParsesCommand()
    {
        // cmd.exe is always available on Windows; without a shell hint the parser falls back to the PATH check.
        TerminalCommandParser.GetFirstTerminalCommand("cmd /c echo hello")
            .Should().Be("cmd /c echo hello");
    }

    [Fact]
    public void GetFirstTerminalCommand_NonexistentBinaryWithoutShellHint_ReturnsNull()
    {
        TerminalCommandParser.GetFirstTerminalCommand("nonexistent_binary_xyz_12345 --flag")
            .Should().BeNull();
    }

    [Fact]
    public void GetFirstTerminalCommand_KnownBinaryWithExtensionWithoutShellHint_ParsesCommand()
    {
        TerminalCommandParser.GetFirstTerminalCommand("cmd.exe /c dir")
            .Should().Be("cmd.exe /c dir");
    }

    // --- GetCommandBlocks tests ---

    [Fact]
    public void GetCommandBlocks_SinglePromptWithContinuation_ReturnsSingleBlock()
    {
        string input = "PS E:\\sources> ls\ndotnet new --install WindowSill.Extension.Template\ndocker --help\nspell";

        List<ParsedCommandBlock> blocks = TerminalCommandParser.GetCommandBlocks(input);

        blocks.Should().HaveCount(1);
        blocks[0].WorkingDirectory.Should().Be("E:\\sources");
        blocks[0].Command.Should().Be("ls\ndotnet new --install WindowSill.Extension.Template\ndocker --help\nspell");
    }

    [Fact]
    public void GetCommandBlocks_TwoPromptsWithContinuation_ReturnsTwoBlocks()
    {
        string input = "PS E:\\sources> ls\ndotnet new --install WindowSill.Extension.Template\nPS E:\\sources2> docker --help\nspell";

        List<ParsedCommandBlock> blocks = TerminalCommandParser.GetCommandBlocks(input);

        blocks.Should().HaveCount(2);
        blocks[0].WorkingDirectory.Should().Be("E:\\sources");
        blocks[0].Command.Should().Be("ls\ndotnet new --install WindowSill.Extension.Template");
        blocks[1].WorkingDirectory.Should().Be("E:\\sources2");
        blocks[1].Command.Should().Be("docker --help\nspell");
    }

    [Fact]
    public void GetCommandBlocks_SinglePromptNoFollowUp_ReturnsSingleLine()
    {
        string input = "PS C:\\code> dotnet build";

        List<ParsedCommandBlock> blocks = TerminalCommandParser.GetCommandBlocks(input);

        blocks.Should().HaveCount(1);
        blocks[0].WorkingDirectory.Should().Be("C:\\code");
        blocks[0].Command.Should().Be("dotnet build");
    }

    [Fact]
    public void GetCommandBlocks_MultiplePromptsNoFollowUp_ReturnsMultipleBlocks()
    {
        string input = "PS C:\\code> dotnet build\nPS C:\\code> dotnet test";

        List<ParsedCommandBlock> blocks = TerminalCommandParser.GetCommandBlocks(input);

        blocks.Should().HaveCount(2);
        blocks[0].Command.Should().Be("dotnet build");
        blocks[1].Command.Should().Be("dotnet test");
    }

    [Fact]
    public void GetCommandBlocks_EmptyText_ReturnsEmpty()
    {
        TerminalCommandParser.GetCommandBlocks("").Should().BeEmpty();
    }

    [Fact]
    public void GetCommandBlocks_NoCommandDetected_ReturnsEmpty()
    {
        TerminalCommandParser.GetCommandBlocks("just some random text").Should().BeEmpty();
    }

    [Fact]
    public void GetCommandBlocks_BashFallback_ReturnsSingleBlock()
    {
        string input = "user@host:~/src$ make build";

        List<ParsedCommandBlock> blocks = TerminalCommandParser.GetCommandBlocks(input);

        blocks.Should().HaveCount(1);
        blocks[0].WorkingDirectory.Should().Be("~/src");
        blocks[0].Command.Should().Be("make build");
    }

    [Fact]
    public void GetCommandBlocks_MarkdownWrappedPrompts_SkipsFences()
    {
        string input = "```powershell\nPS C:\\code> Get-Process\nGet-Service\n```";

        List<ParsedCommandBlock> blocks = TerminalCommandParser.GetCommandBlocks(input);

        blocks.Should().HaveCount(1);
        blocks[0].Command.Should().Be("Get-Process\nGet-Service");
    }

    [Fact]
    public void GetCommandBlocks_PsWithoutWorkingDir_NoWorkingDirectory()
    {
        string input = "PS dotnet build\nPS dotnet test";

        List<ParsedCommandBlock> blocks = TerminalCommandParser.GetCommandBlocks(input);

        blocks.Should().HaveCount(2);
        blocks[0].WorkingDirectory.Should().BeNull();
        blocks[0].Command.Should().Be("dotnet build");
        blocks[1].WorkingDirectory.Should().BeNull();
        blocks[1].Command.Should().Be("dotnet test");
    }
}
