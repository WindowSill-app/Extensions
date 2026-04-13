using FluentAssertions;
using WindowSill.InlineTerminal.Models;
using WindowSill.InlineTerminal.Services;

namespace UnitTests.InlineTerminal;

/// <summary>
/// Additional unit tests for <see cref="CommandService"/> covering edge cases
/// and more complex scenarios.
/// </summary>
public class CommandServiceExtendedTests
{
    #region DismissRun edge cases

    [Fact]
    internal void DismissRun_InvalidCommandId_DoesNothing()
    {
        CommandService service = TestHelpers.CreateCommandService();
        service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);

        // Should not throw with unknown command ID.
        Action act = () => service.DismissRun(Guid.NewGuid(), Guid.NewGuid());
        act.Should().NotThrow();
    }

    [Fact]
    internal void DismissAllRuns_InvalidCommandId_DoesNothing()
    {
        CommandService service = TestHelpers.CreateCommandService();

        Action act = () => service.DismissAllRuns(Guid.NewGuid());
        act.Should().NotThrow();
    }

    [Fact]
    internal void DismissOtherRuns_InvalidCommandId_DoesNothing()
    {
        CommandService service = TestHelpers.CreateCommandService();

        Action act = () => service.DismissOtherRuns(Guid.NewGuid(), Guid.NewGuid());
        act.Should().NotThrow();
    }

    [Fact]
    internal void DismissOtherRuns_NoMatchingKeepId_RemovesAll()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        cmd.Runs.Add(new CommandRun());
        cmd.Runs.Add(new CommandRun());

        // keepRunId doesn't match any run — all get dismissed.
        service.DismissOtherRuns(cmd.Id, Guid.NewGuid());

        cmd.Runs.Should().BeEmpty();
    }

    #endregion

    #region CancelRun

    [Fact]
    internal void CancelRun_InvalidRunId_DoesNothing()
    {
        CommandService service = TestHelpers.CreateCommandService();

        Action act = () => service.CancelRun(Guid.NewGuid());
        act.Should().NotThrow();
    }

    #endregion

    #region Multiple commands

    [Fact]
    internal void CreateCommand_MultipleCommands_AllTracked()
    {
        CommandService service = TestHelpers.CreateCommandService();

        CommandDefinition cmd1 = service.CreateCommand("echo 1", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        CommandDefinition cmd2 = service.CreateCommand("echo 2", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        CommandDefinition cmd3 = service.CreateCommand("echo 3", null, "/tmp", TestHelpers.CreateDummyShell(), null);

        cmd1.Id.Should().NotBe(cmd2.Id);
        cmd2.Id.Should().NotBe(cmd3.Id);
    }

    [Fact]
    internal void DismissAllCommands_MultipleCommandsWithRuns_AllCleared()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd1 = service.CreateCommand("echo 1", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        CommandDefinition cmd2 = service.CreateCommand("echo 2", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        cmd1.Runs.Add(new CommandRun());
        cmd1.Runs.Add(new CommandRun());
        cmd2.Runs.Add(new CommandRun());

        service.DismissAllCommands();

        cmd1.Runs.Should().BeEmpty();
        cmd2.Runs.Should().BeEmpty();
        service.GetAllActiveRuns().Should().BeEmpty();
    }

    #endregion

    #region Execute

    [Fact]
    internal void Execute_AddsRunToCommand()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);

        service.Execute(cmd, TestHelpers.CreateDummyShell(), "echo hi", "/tmp", ActionOnCommandCompleted.None, false);

        cmd.Runs.Should().HaveCount(1);
    }

    [Fact]
    internal void Execute_MultipleExecutions_AddsMultipleRuns()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);

        service.Execute(cmd, TestHelpers.CreateDummyShell(), "echo hi", "/tmp", ActionOnCommandCompleted.None, false);
        service.Execute(cmd, TestHelpers.CreateDummyShell(), "echo hi", "/tmp", ActionOnCommandCompleted.None, false);
        service.Execute(cmd, TestHelpers.CreateDummyShell(), "echo hi", "/tmp", ActionOnCommandCompleted.None, false);

        cmd.Runs.Should().HaveCount(3);
    }

    [Fact]
    internal void Execute_FiresRunsChanged()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);

        bool fired = false;
        service.RunsChanged += (_, _) => fired = true;

        service.Execute(cmd, TestHelpers.CreateDummyShell(), "echo hi", "/tmp", ActionOnCommandCompleted.None, false);

        fired.Should().BeTrue();
    }

    [Fact]
    internal void Execute_UpdatesWorkingDirectory()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);

        service.Execute(cmd, TestHelpers.CreateDummyShell(), "echo hi", "/home/user", ActionOnCommandCompleted.None, false);

        cmd.WorkingDirectory.Should().Be("/home/user");
    }

    [Fact]
    internal void Execute_UpdatesScript()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo old", null, "/tmp", TestHelpers.CreateDummyShell(), null);

        service.Execute(cmd, TestHelpers.CreateDummyShell(), "echo new", "/tmp", ActionOnCommandCompleted.None, false);

        cmd.Script.Should().Be("echo new");
    }

    [Fact]
    internal void Execute_ReturnsNewRunInstance()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);

        CommandRun run = service.Execute(cmd, TestHelpers.CreateDummyShell(), "echo hi", "/tmp", ActionOnCommandCompleted.Copy, false);

        run.Should().NotBeNull();
        run.ActionOnCompleted.Should().Be(ActionOnCommandCompleted.Copy);
    }

    #endregion

    #region GetAllActiveRuns

    [Fact]
    internal void GetAllActiveRuns_NoExecutions_ReturnsEmpty()
    {
        CommandService service = TestHelpers.CreateCommandService();
        service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);

        service.GetAllActiveRuns().Should().BeEmpty();
    }

    [Fact]
    internal void GetAllActiveRuns_WithExecutions_ReturnsAll()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);

        service.Execute(cmd, TestHelpers.CreateDummyShell(), "echo hi", "/tmp", ActionOnCommandCompleted.None, false);
        service.Execute(cmd, TestHelpers.CreateDummyShell(), "echo hi", "/tmp", ActionOnCommandCompleted.None, false);

        service.GetAllActiveRuns().Should().HaveCount(2);
    }

    [Fact]
    internal void GetAllActiveRuns_AfterDismiss_ReflectsRemoval()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);

        CommandRun run = service.Execute(cmd, TestHelpers.CreateDummyShell(), "echo hi", "/tmp", ActionOnCommandCompleted.None, false);

        service.GetAllActiveRuns().Should().HaveCount(1);

        service.DismissRun(cmd.Id, run.Id);

        service.GetAllActiveRuns().Should().BeEmpty();
    }

    #endregion

    #region Event ordering

    [Fact]
    internal void DismissRun_LastRun_FiresBothRunsChangedAndCommandRemoved()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        var run = new CommandRun();
        cmd.Runs.Add(run);

        var events = new List<string>();
        service.RunsChanged += (_, _) => events.Add("RunsChanged");
        service.CommandRemoved += (_, _) => events.Add("CommandRemoved");
        service.CommandsChanged += (_, _) => events.Add("CommandsChanged");

        service.DismissRun(cmd.Id, run.Id);

        events.Should().Contain("RunsChanged");
        events.Should().Contain("CommandRemoved");
        events.Should().Contain("CommandsChanged");
    }

    #endregion
}
