using FluentAssertions;
using WindowSill.InlineTerminal.Core.Shell;
using WindowSill.InlineTerminal.Models;

namespace UnitTests.InlineTerminal;

/// <summary>
/// Unit tests for <see cref="CommandDefinition"/>.
/// </summary>
public class CommandDefinitionTests
{
    private static ShellInfo CreateDummyShell()
    {
        return new ShellInfo(
            "TestShell",
            "/usr/bin/test",
            escapeCommand: c => c,
            buildArguments: c => $"-c {c}",
            buildElevatedArguments: (c, f) => $"-c {c} > {f}");
    }

    [Fact]
    internal void Constructor_WithScript_SetsProperties()
    {
        var cmd = new CommandDefinition("echo hello", null, "/tmp", CreateDummyShell(), null);

        cmd.Id.Should().NotBeEmpty();
        cmd.Script.Should().Be("echo hello");
        cmd.ScriptFilePath.Should().BeNull();
        cmd.WorkingDirectory.Should().Be("/tmp");
    }

    [Fact]
    internal void Constructor_WithScriptFilePath_SetsProperties()
    {
        var cmd = new CommandDefinition(null, "/scripts/run.sh", null, CreateDummyShell(), null);

        cmd.ScriptFilePath.Should().Be("/scripts/run.sh");
        cmd.Script.Should().BeNull();
    }

    [Fact]
    internal void Constructor_NoScriptOrPath_Throws()
    {
        Action act = () => new CommandDefinition(null, null, null, CreateDummyShell(), null);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    internal void Constructor_NullWorkingDirectory_DefaultsToUserProfile()
    {
        var cmd = new CommandDefinition("ls", null, null, CreateDummyShell(), null);
        cmd.WorkingDirectory.Should().Be(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    }

    [Fact]
    internal void Title_WithScript_ReturnsScriptPrefix()
    {
        var cmd = new CommandDefinition("echo hello world", null, "/tmp", CreateDummyShell(), null);
        cmd.Title.Should().Be("echo hello world");
    }

    [Fact]
    internal void Title_WithLongScript_TruncatesAt100()
    {
        string longScript = new string('a', 200);
        var cmd = new CommandDefinition(longScript, null, "/tmp", CreateDummyShell(), null);
        cmd.Title.Should().HaveLength(100);
    }

    [Fact]
    internal void Title_WithScriptFilePath_ReturnsFileName()
    {
        var cmd = new CommandDefinition(null, "/scripts/deploy.sh", null, CreateDummyShell(), null);
        cmd.Title.Should().Be("deploy.sh");
    }

    [Fact]
    internal void Title_WithNewlines_ReplacesWithSymbol()
    {
        var cmd = new CommandDefinition("line1\nline2\r\nline3", null, "/tmp", CreateDummyShell(), null);
        cmd.Title.Should().Contain("⏎");
        cmd.Title.Should().NotContain("\n");
        cmd.Title.Should().NotContain("\r");
    }

    [Fact]
    internal void HasBeenExecuted_NoRuns_ReturnsFalse()
    {
        var cmd = new CommandDefinition("ls", null, "/tmp", CreateDummyShell(), null);
        cmd.HasBeenExecuted.Should().BeFalse();
    }

    [Fact]
    internal void HasBeenExecuted_WithRun_ReturnsTrue()
    {
        var cmd = new CommandDefinition("ls", null, "/tmp", CreateDummyShell(), null);
        cmd.Runs.Add(new CommandRun());
        cmd.HasBeenExecuted.Should().BeTrue();
    }

    [Fact]
    internal void LatestRun_NoRuns_ReturnsNull()
    {
        var cmd = new CommandDefinition("ls", null, "/tmp", CreateDummyShell(), null);
        cmd.LatestRun.Should().BeNull();
    }

    [Fact]
    internal void LatestRun_MultipleRuns_ReturnsLast()
    {
        var cmd = new CommandDefinition("ls", null, "/tmp", CreateDummyShell(), null);
        var run1 = new CommandRun();
        var run2 = new CommandRun();
        cmd.Runs.Add(run1);
        cmd.Runs.Add(run2);
        cmd.LatestRun.Should().BeSameAs(run2);
    }
}
