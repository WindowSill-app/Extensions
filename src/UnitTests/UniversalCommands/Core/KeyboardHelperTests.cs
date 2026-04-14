using FluentAssertions;
using Windows.System;
using WindowSill.UniversalCommands.Core;

namespace UnitTests.UniversalCommands.Core;

public class KeyboardHelperTests
{
    [Theory]
    [InlineData(VirtualKey.Control, true)]
    [InlineData(VirtualKey.LeftControl, true)]
    [InlineData(VirtualKey.RightControl, true)]
    [InlineData(VirtualKey.Menu, true)]
    [InlineData(VirtualKey.LeftMenu, true)]
    [InlineData(VirtualKey.RightMenu, true)]
    [InlineData(VirtualKey.Shift, true)]
    [InlineData(VirtualKey.LeftShift, true)]
    [InlineData(VirtualKey.RightShift, true)]
    [InlineData(VirtualKey.LeftWindows, true)]
    [InlineData(VirtualKey.RightWindows, true)]
    [InlineData(VirtualKey.A, false)]
    [InlineData(VirtualKey.Enter, false)]
    [InlineData(VirtualKey.Space, false)]
    [InlineData(VirtualKey.F1, false)]
    [InlineData(VirtualKey.Tab, false)]
    [InlineData(VirtualKey.Escape, false)]
    [InlineData(VirtualKey.Number0, false)]
    [InlineData(VirtualKey.Delete, false)]
    public void IsModifierKey(VirtualKey key, bool expected)
    {
        KeyboardHelper.IsModifierKey(key).Should().Be(expected);
    }

    [Theory]
    [InlineData(VirtualKey.LeftControl, VirtualKey.Control)]
    [InlineData(VirtualKey.RightControl, VirtualKey.Control)]
    [InlineData(VirtualKey.LeftMenu, VirtualKey.Menu)]
    [InlineData(VirtualKey.RightMenu, VirtualKey.Menu)]
    [InlineData(VirtualKey.LeftShift, VirtualKey.Shift)]
    [InlineData(VirtualKey.RightShift, VirtualKey.Shift)]
    public void NormalizeModifier_LeftRightVariants_NormalizesToGeneric(VirtualKey input, VirtualKey expected)
    {
        KeyboardHelper.NormalizeModifier(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(VirtualKey.Control)]
    [InlineData(VirtualKey.Menu)]
    [InlineData(VirtualKey.Shift)]
    [InlineData(VirtualKey.A)]
    [InlineData(VirtualKey.F5)]
    [InlineData(VirtualKey.Space)]
    [InlineData(VirtualKey.Enter)]
    [InlineData(VirtualKey.LeftWindows)]
    [InlineData(VirtualKey.RightWindows)]
    public void NormalizeModifier_NonLeftRightVariants_ReturnsUnchanged(VirtualKey key)
    {
        KeyboardHelper.NormalizeModifier(key).Should().Be(key);
    }
}
