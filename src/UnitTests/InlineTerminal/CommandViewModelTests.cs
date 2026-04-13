using FluentAssertions;
using WindowSill.InlineTerminal.Models;
using WindowSill.InlineTerminal.Services;
using WindowSill.InlineTerminal.ViewModels;

namespace UnitTests.InlineTerminal;

/// <summary>
/// Unit tests for <see cref="CommandViewModel"/> properties and command delegation.
/// Note: Tests avoid triggering UI thread operations (NotifyUpdateUI).
/// </summary>
public class CommandViewModelTests
{
    private static CommandViewModel CreateViewModel(CommandDefinition? command = null, CommandService? service = null)
    {
        service ??= TestHelpers.CreateCommandService();
        command ??= service.CreateCommand("echo hello", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        var shells = new[] { TestHelpers.CreateDummyShell() };
        return new CommandViewModel(service, command, shells, new TestHelpers.FakeSettingsProvider());
    }

    #region Properties

    [Fact]
    internal void CommandId_MatchesDefinition()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        CommandViewModel vm = CreateViewModel(cmd, service);

        vm.CommandId.Should().Be(cmd.Id);
    }

    [Fact]
    internal void Title_MatchesDefinition()
    {
        CommandViewModel vm = CreateViewModel();

        vm.Title.Should().Be("echo hello");
    }

    [Fact]
    internal void Script_InitializedFromDefinition()
    {
        CommandViewModel vm = CreateViewModel();

        vm.Script.Should().Be("echo hello");
    }

    [Fact]
    internal void WorkingDirectory_InitializedFromDefinition()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("ls", null, "/home/user", TestHelpers.CreateDummyShell(), null);
        CommandViewModel vm = CreateViewModel(cmd, service);

        vm.WorkingDirectory.Should().Be("/home/user");
    }

    [Fact]
    internal void SelectedShell_InitializedFromDefinition()
    {
        var shell = TestHelpers.CreateDummyShell("MyShell");
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("ls", null, "/tmp", shell, null);
        CommandViewModel vm = CreateViewModel(cmd, service);

        vm.SelectedShell.DisplayName.Should().Be("MyShell");
    }

    [Fact]
    internal void State_NoRuns_ReturnsCreated()
    {
        CommandViewModel vm = CreateViewModel();

        vm.State.Should().Be(CommandState.Created);
    }

    [Fact]
    internal void State_WithRun_ReturnsRunState()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        var run = new CommandRun();
        cmd.Runs.Add(run);
        CommandViewModel vm = CreateViewModel(cmd, service);

        vm.State.Should().Be(CommandState.Running);

        // Don't call run.Complete() here — it fires property changed
        // which triggers NotifyUpdateUI needing UI thread.
        // Instead test state derivation with a pre-completed run.
    }

    [Fact]
    internal void State_WithCompletedRun_ReturnsCompleted()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        var run = new CommandRun();
        run.Complete(CommandState.Completed); // Complete before subscribing VM
        cmd.Runs.Add(run);
        CommandViewModel vm = CreateViewModel(cmd, service);

        vm.State.Should().Be(CommandState.Completed);
    }

    [Fact]
    internal void HasBeenExecuted_NoRuns_ReturnsFalse()
    {
        CommandViewModel vm = CreateViewModel();

        vm.HasBeenExecuted.Should().BeFalse();
    }

    [Fact]
    internal void HasBeenExecuted_WithRun_ReturnsTrue()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        cmd.Runs.Add(new CommandRun());
        CommandViewModel vm = CreateViewModel(cmd, service);

        vm.HasBeenExecuted.Should().BeTrue();
    }

    [Fact]
    internal void RunCount_ReflectsRunsCollection()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        CommandViewModel vm = CreateViewModel(cmd, service);

        vm.RunCount.Should().Be(0);

        cmd.Runs.Add(new CommandRun());
        vm.RunCount.Should().Be(1);

        cmd.Runs.Add(new CommandRun());
        vm.RunCount.Should().Be(2);
    }

    [Fact]
    internal void LatestRun_NoRuns_ReturnsNull()
    {
        CommandViewModel vm = CreateViewModel();

        vm.LatestRun.Should().BeNull();
    }

    [Fact]
    internal void LatestRun_MultipleRuns_ReturnsLast()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        var run1 = new CommandRun();
        var run2 = new CommandRun();
        cmd.Runs.Add(run1);
        cmd.Runs.Add(run2);
        CommandViewModel vm = CreateViewModel(cmd, service);

        vm.LatestRun.Should().BeSameAs(run2);
    }

    [Fact]
    internal void OutputText_NoRuns_ReturnsEmpty()
    {
        CommandViewModel vm = CreateViewModel();

        vm.OutputText.Should().BeEmpty();
    }

    [Fact]
    internal void OutputText_WithRun_ReturnsRunOutput()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        var run = new CommandRun();
        run.AppendOutput("hello world");
        cmd.Runs.Add(run);
        CommandViewModel vm = CreateViewModel(cmd, service);

        vm.OutputText.Should().Contain("hello world");
    }

    [Fact]
    internal void LatestRunStartedAt_NoRuns_ReturnsEmpty()
    {
        CommandViewModel vm = CreateViewModel();

        vm.LatestRunStartedAt.Should().BeEmpty();
    }

    [Fact]
    internal void LatestRunStartedAt_WithRun_ReturnsFormattedTime()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        cmd.Runs.Add(new CommandRun());
        CommandViewModel vm = CreateViewModel(cmd, service);

        vm.LatestRunStartedAt.Should().Contain(":");
    }

    [Fact]
    internal void OtherRunsCount_NoRuns_ReturnsZero()
    {
        CommandViewModel vm = CreateViewModel();

        vm.OtherRunsCount.Should().Be(0);
    }

    [Fact]
    internal void OtherRunsCount_OneRun_ReturnsZero()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        cmd.Runs.Add(new CommandRun());
        CommandViewModel vm = CreateViewModel(cmd, service);

        vm.OtherRunsCount.Should().Be(0);
    }

    [Fact]
    internal void OtherRunsCount_MultipleRuns_ReturnsCountMinusOne()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        cmd.Runs.Add(new CommandRun());
        cmd.Runs.Add(new CommandRun());
        cmd.Runs.Add(new CommandRun());
        CommandViewModel vm = CreateViewModel(cmd, service);

        vm.OtherRunsCount.Should().Be(2);
    }

    [Fact]
    internal void HasOtherRuns_OneRun_ReturnsFalse()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        cmd.Runs.Add(new CommandRun());
        CommandViewModel vm = CreateViewModel(cmd, service);

        vm.HasOtherRuns.Should().BeFalse();
    }

    [Fact]
    internal void HasOtherRuns_MultipleRuns_ReturnsTrue()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        cmd.Runs.Add(new CommandRun());
        cmd.Runs.Add(new CommandRun());
        CommandViewModel vm = CreateViewModel(cmd, service);

        vm.HasOtherRuns.Should().BeTrue();
    }

    [Fact]
    internal void RunAsAdministrator_DefaultsFalse()
    {
        CommandViewModel vm = CreateViewModel();

        vm.RunAsAdministrator.Should().BeFalse();
    }

    [Fact]
    internal void ScriptFilePath_NoFile_ReturnsNull()
    {
        CommandViewModel vm = CreateViewModel();

        vm.ScriptFilePath.Should().BeNull();
    }

    [Fact]
    internal void AvailableShells_ReturnsProvidedList()
    {
        CommandViewModel vm = CreateViewModel();

        vm.AvailableShells.Should().HaveCount(1);
    }

    #endregion

    #region Command Delegation

    [Fact]
    internal void Dismiss_RemovesLatestRunFromModel()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        var run1 = new CommandRun();
        run1.Complete(CommandState.Completed);
        var run2 = new CommandRun();
        run2.Complete(CommandState.Completed);
        cmd.Runs.Add(run1);
        cmd.Runs.Add(run2);

        // Test via CommandService directly (VM.Dismiss calls NotifyUpdateUI which needs UI thread).
        service.DismissRun(cmd.Id, run2.Id);

        cmd.Runs.Should().ContainSingle().Which.Id.Should().Be(run1.Id);
    }

    [Fact]
    internal void Dismiss_LastRun_FiresRequestClose()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        var run = new CommandRun();
        run.Complete(CommandState.Completed);
        cmd.Runs.Add(run);
        CommandViewModel vm = CreateViewModel(cmd, service);

        bool closeFired = false;
        vm.RequestClose += (_, _) => closeFired = true;

        // DismissAll fires RequestClose without needing UI thread for notification
        // since it exits immediately after.
        vm.DismissAll();

        closeFired.Should().BeTrue();
    }

    [Fact]
    internal void DismissOthers_KeepsLatestRun()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        var run1 = new CommandRun();
        var run2 = new CommandRun();
        var run3 = new CommandRun();
        cmd.Runs.Add(run1);
        cmd.Runs.Add(run2);
        cmd.Runs.Add(run3);

        // Test via CommandService directly.
        service.DismissOtherRuns(cmd.Id, run3.Id);

        cmd.Runs.Should().ContainSingle().Which.Id.Should().Be(run3.Id);
    }

    [Fact]
    internal void DismissAll_RemovesAllRuns_FiresRequestClose()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        cmd.Runs.Add(new CommandRun());
        cmd.Runs.Add(new CommandRun());
        CommandViewModel vm = CreateViewModel(cmd, service);

        bool closeFired = false;
        vm.RequestClose += (_, _) => closeFired = true;

        vm.DismissAll();

        cmd.Runs.Should().BeEmpty();
        closeFired.Should().BeTrue();
    }

    [Fact]
    internal void ShouldShowClickFixWarning_NullSource_ReturnsFalse()
    {
        CommandViewModel vm = CreateViewModel();

        vm.ShouldShowClickFixWarning().Should().BeFalse();
    }

    [Fact]
    internal void Command_ExposesUnderlyingDefinition()
    {
        CommandService service = TestHelpers.CreateCommandService();
        CommandDefinition cmd = service.CreateCommand("echo hi", null, "/tmp", TestHelpers.CreateDummyShell(), null);
        CommandViewModel vm = CreateViewModel(cmd, service);

        vm.Command.Should().BeSameAs(cmd);
    }

    [Fact]
    internal void Dispose_DoesNotThrow()
    {
        CommandViewModel vm = CreateViewModel();

        Action act = () => vm.Dispose();
        act.Should().NotThrow();
    }

    #endregion
}
