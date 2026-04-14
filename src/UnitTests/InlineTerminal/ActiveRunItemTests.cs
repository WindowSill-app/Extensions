using FluentAssertions;
using WindowSill.InlineTerminal.Models;
using WindowSill.InlineTerminal.ViewModels;

namespace UnitTests.InlineTerminal;

/// <summary>
/// Unit tests for <see cref="ActiveRunItem"/>.
/// </summary>
public class ActiveRunItemTests
{
    [Fact]
    internal void Title_DelegatesToCommandDefinition()
    {
        var command = new CommandDefinition("echo hello", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        var run = new CommandRun();
        var item = new ActiveRunItem(command, run);

        item.Title.Should().Be("echo hello");
    }

    [Fact]
    internal void State_DelegatesToRun()
    {
        var command = new CommandDefinition("echo hello", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        var run = new CommandRun();
        var item = new ActiveRunItem(command, run);

        item.State.Should().Be(CommandState.Running);

        run.Complete(CommandState.Completed);
        item.State.Should().Be(CommandState.Completed);
    }

    [Fact]
    internal void OutputTrimmed_DelegatesToRun()
    {
        var command = new CommandDefinition("echo hello", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        var run = new CommandRun();
        var item = new ActiveRunItem(command, run);

        item.OutputTrimmed.Should().BeEmpty();

        run.AppendOutput("  some output  ");
        item.OutputTrimmed.Should().StartWith("some output");
    }

    [Fact]
    internal void StartedAt_ReturnsFormattedTimeWithSeconds()
    {
        var command = new CommandDefinition("echo hello", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        var run = new CommandRun();
        var item = new ActiveRunItem(command, run);

        // "T" format includes seconds — should have at least 2 colons (HH:MM:SS)
        item.StartedAt.Should().Contain(":");
        item.StartedAt.Split(':').Length.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    internal void Command_ReturnsOriginalDefinition()
    {
        var command = new CommandDefinition("echo hello", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        var run = new CommandRun();
        var item = new ActiveRunItem(command, run);

        item.Command.Should().BeSameAs(command);
    }

    [Fact]
    internal void Run_ReturnsOriginalRun()
    {
        var command = new CommandDefinition("echo hello", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        var run = new CommandRun();
        var item = new ActiveRunItem(command, run);

        item.Run.Should().BeSameAs(run);
    }

    [Fact]
    internal void State_ReflectsRunStateChanges()
    {
        var command = new CommandDefinition("ls", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        var run = new CommandRun();
        var item = new ActiveRunItem(command, run);

        item.State.Should().Be(CommandState.Running);

        run.Complete(CommandState.Failed);
        item.State.Should().Be(CommandState.Failed);
    }
}
