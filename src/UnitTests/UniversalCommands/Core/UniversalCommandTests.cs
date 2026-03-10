using FluentAssertions;
using Windows.System;
using WindowSill.UniversalCommands.Core;

namespace UnitTests.UniversalCommands.Core;

public class UniversalCommandTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var command = new UniversalCommand();

        command.Id.Should().NotBeNullOrWhiteSpace();
        command.Id.Should().HaveLength(32, "GUID with 'N' format is 32 hex chars");
        command.Name.Should().BeEmpty();
        command.Type.Should().Be(UniversalCommandType.KeyboardShortcut);
        command.KeyboardChord.Should().BeEmpty();
        command.PowerShellCommand.Should().BeNull();
        command.TargetAppProcessName.Should().BeNull();
        command.IconGlyph.Should().Be('\uE768');
        command.IconImagePath.Should().BeNull();
    }

    [Fact]
    public void KeyboardChordKeys_EmptyChord_ReturnsEmptyList()
    {
        var command = new UniversalCommand { KeyboardChord = [] };

        command.KeyboardChordKeys.Should().BeEmpty();
    }

    [Fact]
    public void KeyboardChordKeys_SingleCombo_ConvertsToVirtualKeys()
    {
        // Ctrl+C
        var command = new UniversalCommand
        {
            KeyboardChord = [[(int)VirtualKey.Control, (int)VirtualKey.C]]
        };

        command.KeyboardChordKeys.Should().HaveCount(1);
        command.KeyboardChordKeys[0].Should().ContainInOrder(VirtualKey.Control, VirtualKey.C);
    }

    [Fact]
    public void KeyboardChordKeys_MultiStepChord_ConvertsAllCombos()
    {
        // Ctrl+K, Ctrl+D
        var command = new UniversalCommand
        {
            KeyboardChord =
            [
                [(int)VirtualKey.Control, (int)VirtualKey.K],
                [(int)VirtualKey.Control, (int)VirtualKey.D]
            ]
        };

        command.KeyboardChordKeys.Should().HaveCount(2);
        command.KeyboardChordKeys[0].Should().ContainInOrder(VirtualKey.Control, VirtualKey.K);
        command.KeyboardChordKeys[1].Should().ContainInOrder(VirtualKey.Control, VirtualKey.D);
    }

    [Fact]
    public void KeyboardChordKeys_SingleKey_ConvertsCorrectly()
    {
        // F5 (single key, no modifiers)
        var command = new UniversalCommand
        {
            KeyboardChord = [[(int)VirtualKey.F5]]
        };

        command.KeyboardChordKeys.Should().HaveCount(1);
        command.KeyboardChordKeys[0].Should().ContainInOrder(VirtualKey.F5);
    }

    [Fact]
    public void KeyboardChordKeys_ThreeModifiersAndKey_ConvertsCorrectly()
    {
        // Ctrl+Shift+Alt+P
        var command = new UniversalCommand
        {
            KeyboardChord =
            [
                [(int)VirtualKey.Control, (int)VirtualKey.Shift, (int)VirtualKey.Menu, (int)VirtualKey.P]
            ]
        };

        IReadOnlyList<VirtualKey> combo = command.KeyboardChordKeys[0];
        combo.Should().HaveCount(4);
        combo.Should().ContainInOrder(VirtualKey.Control, VirtualKey.Shift, VirtualKey.Menu, VirtualKey.P);
    }

    [Fact]
    public void EachInstance_HasUniqueId()
    {
        var command1 = new UniversalCommand();
        var command2 = new UniversalCommand();

        command1.Id.Should().NotBe(command2.Id);
    }
}
