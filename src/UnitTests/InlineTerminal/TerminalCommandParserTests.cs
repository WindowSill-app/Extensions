using FluentAssertions;
using WindowSill.InlineTerminal.Core.Parsers;
using WindowSill.InlineTerminal.Models;

namespace UnitTests.InlineTerminal;

/// <summary>
/// Unit tests for <see cref="TerminalCommandParser"/>.
/// </summary>
public class TerminalCommandParserTests
{
    #region PowerShell prompt parsing

    [Fact]
    internal void GetCommandBlocks_PsPromptWithWorkingDir_ParsesCorrectly()
    {
        List<ParsedCommandBlock> blocks = TerminalCommandParser.GetCommandBlocks("PS C:\\Users\\me> dotnet build");
        blocks.Should().HaveCount(1);
        blocks[0].Command.Should().Be("dotnet build");
        blocks[0].WorkingDirectory.Should().Be("C:\\Users\\me");
    }

    [Fact]
    internal void GetCommandBlocks_PsPromptWithoutWorkingDir_ParsesCommand()
    {
        List<ParsedCommandBlock> blocks = TerminalCommandParser.GetCommandBlocks("PS dotnet build");
        blocks.Should().HaveCount(1);
        blocks[0].Command.Should().Be("dotnet build");
        blocks[0].WorkingDirectory.Should().BeNull();
    }

    [Fact]
    internal void GetCommandBlocks_MultiplePsPrompts_ParsesAll()
    {
        string text = "PS C:\\> cd src\nPS C:\\src> dotnet build";
        List<ParsedCommandBlock> blocks = TerminalCommandParser.GetCommandBlocks(text);
        blocks.Should().HaveCount(2);
        blocks[0].Command.Should().Be("cd src");
        blocks[1].Command.Should().Be("dotnet build");
    }

    [Fact]
    internal void GetCommandBlocks_MultiLineCommand_JoinsLines()
    {
        string text = "PS C:\\> dotnet build\n--configuration Release";
        List<ParsedCommandBlock> blocks = TerminalCommandParser.GetCommandBlocks(text);
        blocks.Should().HaveCount(1);
        blocks[0].Command.Should().Contain("dotnet build");
        blocks[0].Command.Should().Contain("--configuration Release");
    }

    [Fact]
    internal void GetCommandBlocks_MultiplePsPromptsSameWorkingDir_MergesIntoOneBlock()
    {
        string text = "PS C:\\src> dotnet build\nPS C:\\src> dotnet test";
        List<ParsedCommandBlock> blocks = TerminalCommandParser.GetCommandBlocks(text);
        blocks.Should().HaveCount(1);
        blocks[0].WorkingDirectory.Should().Be("C:\\src");
        blocks[0].Command.Should().Be("dotnet build\ndotnet test");
    }

    #endregion

    #region Bare path prompt parsing

    [Fact]
    internal void GetCommandBlocks_BarePathPrompt_ParsesWorkingDirAndCommand()
    {
        string text = "E:\\source\\WindowSill-app> Start-Sleep 5";
        List<ParsedCommandBlock> blocks = TerminalCommandParser.GetCommandBlocks(text);
        blocks.Should().HaveCount(1);
        blocks[0].WorkingDirectory.Should().Be("E:\\source\\WindowSill-app");
        blocks[0].Command.Should().Be("Start-Sleep 5");
    }

    [Fact]
    internal void GetCommandBlocks_BarePathPromptSameDir_MergesIntoOneBlock()
    {
        string text = "E:\\source\\WindowSill-app> Start-Sleep 5\nE:\\source\\WindowSill-app> ls Extensions";
        List<ParsedCommandBlock> blocks = TerminalCommandParser.GetCommandBlocks(text);
        blocks.Should().HaveCount(1);
        blocks[0].WorkingDirectory.Should().Be("E:\\source\\WindowSill-app");
        blocks[0].Command.Should().Be("Start-Sleep 5\nls Extensions");
    }

    [Fact]
    internal void GetCommandBlocks_BarePathPromptDifferentDirs_SplitsIntoTwoBlocks()
    {
        string text = "E:\\source\\WindowSill-app> Start-Sleep 5\nE:\\source\\other> ls Extensions";
        List<ParsedCommandBlock> blocks = TerminalCommandParser.GetCommandBlocks(text);
        blocks.Should().HaveCount(2);
        blocks[0].WorkingDirectory.Should().Be("E:\\source\\WindowSill-app");
        blocks[0].Command.Should().Be("Start-Sleep 5");
        blocks[1].WorkingDirectory.Should().Be("E:\\source\\other");
        blocks[1].Command.Should().Be("ls Extensions");
    }

    #endregion

    #region Bash prompt parsing

    [Fact]
    internal void GetCommandBlocks_BashPrompt_ParsesCorrectly()
    {
        List<ParsedCommandBlock> blocks = TerminalCommandParser.GetCommandBlocks("user@host:~/projects$ make install");
        blocks.Should().HaveCount(1);
        blocks[0].Command.Should().Be("make install");
        blocks[0].WorkingDirectory.Should().Be("~/projects");
    }

    #endregion

    #region Markdown code blocks

    [Fact]
    internal void GetCommandBlocks_MarkdownPsBlock_SkipsFenceLines()
    {
        string text = "```powershell\nPS C:\\> Get-Process\n```";
        List<ParsedCommandBlock> blocks = TerminalCommandParser.GetCommandBlocks(text);
        blocks.Should().HaveCount(1);
        blocks[0].Command.Should().Be("Get-Process");
    }

    #endregion

    #region Empty / no commands

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("just some random text")]
    internal void GetCommandBlocks_NoRecognizedCommands_ReturnsEmpty(string text)
    {
        TerminalCommandParser.GetCommandBlocks(text).Should().BeEmpty();
    }

    #endregion

    #region Helper methods

    [Fact]
    internal void GetFirstTerminalCommand_ValidPrompt_ReturnsFirst()
    {
        TerminalCommandParser.GetFirstTerminalCommand("PS C:\\> echo hello").Should().Be("echo hello");
    }

    [Fact]
    internal void GetFirstTerminalCommand_NoCommand_ReturnsNull()
    {
        TerminalCommandParser.GetFirstTerminalCommand("random text").Should().BeNull();
    }

    [Fact]
    internal void GetFirstWorkingDirectory_ValidPrompt_ReturnsFirst()
    {
        TerminalCommandParser.GetFirstWorkingDirectory("PS C:\\src> build").Should().Be("C:\\src");
    }

    [Fact]
    internal void GetFirstWorkingDirectory_NoPrompt_ReturnsNull()
    {
        TerminalCommandParser.GetFirstWorkingDirectory("random text").Should().BeNull();
    }

    #endregion
}
