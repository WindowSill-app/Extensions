using FluentAssertions;
using WindowSill.API;
using WindowSill.InlineTerminal.Core.Shell;
using WindowSill.InlineTerminal.Models;
using WindowSill.InlineTerminal.Services;

namespace UnitTests.InlineTerminal;

/// <summary>
/// Unit tests for <see cref="CommandService"/> lifecycle operations.
/// Tests focus on command/run management, not actual process execution.
/// </summary>
public class CommandServiceTests
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

    private static CommandService CreateService()
    {
        return new CommandService(new FakePluginInfo(), new FakeProcessInteractionService());
    }

    #region CreateCommand

    [Fact]
    internal void CreateCommand_ReturnsDefinitionWithId()
    {
        CommandService service = CreateService();

        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", CreateDummyShell(), null);

        cmd.Should().NotBeNull();
        cmd.Id.Should().NotBeEmpty();
        cmd.Script.Should().Be("echo hi");
    }

    [Fact]
    internal void CreateCommand_FiresCommandsChanged()
    {
        CommandService service = CreateService();
        bool fired = false;
        service.CommandsChanged += (_, _) => fired = true;

        service.CreateCommand("ls", null, "/tmp", CreateDummyShell(), null);

        fired.Should().BeTrue();
    }

    #endregion

    #region DismissRun

    [Fact]
    internal void DismissRun_RemovesRunFromCommand()
    {
        CommandService service = CreateService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", CreateDummyShell(), null);

        var run = new CommandRun();
        cmd.Runs.Add(run);

        service.DismissRun(cmd.Id, run.Id);

        cmd.Runs.Should().BeEmpty();
    }

    [Fact]
    internal void DismissRun_LastRun_FiresCommandRemoved()
    {
        CommandService service = CreateService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", CreateDummyShell(), null);
        var run = new CommandRun();
        cmd.Runs.Add(run);

        bool commandRemoved = false;
        service.CommandRemoved += (_, id) => { if (id == cmd.Id) commandRemoved = true; };

        service.DismissRun(cmd.Id, run.Id);

        commandRemoved.Should().BeTrue();
    }

    [Fact]
    internal void DismissRun_NotLastRun_KeepsOtherRuns()
    {
        CommandService service = CreateService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", CreateDummyShell(), null);
        var run1 = new CommandRun();
        var run2 = new CommandRun();
        cmd.Runs.Add(run1);
        cmd.Runs.Add(run2);

        service.DismissRun(cmd.Id, run1.Id);

        cmd.Runs.Should().ContainSingle().Which.Id.Should().Be(run2.Id);
    }

    [Fact]
    internal void DismissRun_InvalidRunId_DoesNothing()
    {
        CommandService service = CreateService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", CreateDummyShell(), null);
        var run = new CommandRun();
        cmd.Runs.Add(run);

        service.DismissRun(cmd.Id, Guid.NewGuid());

        cmd.Runs.Should().ContainSingle();
    }

    [Fact]
    internal void DismissRun_FiresRunsChanged()
    {
        CommandService service = CreateService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", CreateDummyShell(), null);
        var run = new CommandRun();
        cmd.Runs.Add(run);

        bool fired = false;
        service.RunsChanged += (_, _) => fired = true;

        service.DismissRun(cmd.Id, run.Id);

        fired.Should().BeTrue();
    }

    #endregion

    #region DismissAllRuns

    [Fact]
    internal void DismissAllRuns_RemovesAllRuns()
    {
        CommandService service = CreateService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", CreateDummyShell(), null);
        cmd.Runs.Add(new CommandRun());
        cmd.Runs.Add(new CommandRun());

        service.DismissAllRuns(cmd.Id);

        cmd.Runs.Should().BeEmpty();
    }

    [Fact]
    internal void DismissAllRuns_FiresCommandRemoved()
    {
        CommandService service = CreateService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", CreateDummyShell(), null);
        cmd.Runs.Add(new CommandRun());

        bool removed = false;
        service.CommandRemoved += (_, id) => { if (id == cmd.Id) removed = true; };

        service.DismissAllRuns(cmd.Id);

        removed.Should().BeTrue();
    }

    #endregion

    #region DismissOtherRuns

    [Fact]
    internal void DismissOtherRuns_KeepsSpecifiedRun()
    {
        CommandService service = CreateService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", CreateDummyShell(), null);
        var run1 = new CommandRun();
        var run2 = new CommandRun();
        var run3 = new CommandRun();
        cmd.Runs.Add(run1);
        cmd.Runs.Add(run2);
        cmd.Runs.Add(run3);

        service.DismissOtherRuns(cmd.Id, run2.Id);

        cmd.Runs.Should().ContainSingle().Which.Id.Should().Be(run2.Id);
    }

    [Fact]
    internal void DismissOtherRuns_SingleRun_DoesNothing()
    {
        CommandService service = CreateService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", CreateDummyShell(), null);
        var run = new CommandRun();
        cmd.Runs.Add(run);

        service.DismissOtherRuns(cmd.Id, run.Id);

        cmd.Runs.Should().ContainSingle();
    }

    #endregion

    #region DismissAllCommands

    [Fact]
    internal void DismissAllCommands_RemovesEverything()
    {
        CommandService service = CreateService();
        CommandDefinition cmd1 = service.CreateCommand("echo 1", null, "/tmp", CreateDummyShell(), null);
        CommandDefinition cmd2 = service.CreateCommand("echo 2", null, "/tmp", CreateDummyShell(), null);
        cmd1.Runs.Add(new CommandRun());
        cmd2.Runs.Add(new CommandRun());

        service.DismissAllCommands();

        cmd1.Runs.Should().BeEmpty();
        cmd2.Runs.Should().BeEmpty();
        service.GetAllActiveRuns().Should().BeEmpty();
    }

    #endregion

    #region Execute re-registers dismissed command

    [Fact]
    internal void Execute_DismissedCommand_ReregistersIt()
    {
        CommandService service = CreateService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", CreateDummyShell(), null);
        cmd.Runs.Add(new CommandRun());

        service.DismissAllRuns(cmd.Id);

        // Execute should re-register the command.
        service.Execute(cmd, CreateDummyShell(), "echo hi", "/tmp", ActionOnCommandCompleted.None, false);

        cmd.Runs.Should().NotBeEmpty();
    }

    #endregion

    #region HasRunningRuns

    [Fact]
    internal void HasRunningRuns_NoRuns_ReturnsFalse()
    {
        CommandService service = CreateService();
        service.HasRunningRuns().Should().BeFalse();
    }

    #endregion

    #region Fakes

    private sealed class FakePluginInfo : IPluginInfo
    {
        public string GetPluginContentDirectory() => System.IO.Path.GetTempPath();
        public string GetPluginDataFolder() => System.IO.Path.GetTempPath();
        public string GetPluginTempFolder() => System.IO.Path.GetTempPath();
    }

    private sealed class FakeProcessInteractionService : IProcessInteractionService
    {
        public Task SimulateKeysOnWindow(WindowInfo window, params Windows.System.VirtualKey[] keys)
            => Task.CompletedTask;
        public Task SimulateKeysOnLastActiveWindow(params Windows.System.VirtualKey[] keys)
            => Task.CompletedTask;
    }

    #endregion
}
