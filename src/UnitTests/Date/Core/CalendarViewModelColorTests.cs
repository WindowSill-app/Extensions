using FluentAssertions;
using UnitTests.Date.Core.Fakes;
using WindowSill.Date.Core.Models;
using WindowSill.Date.ViewModels;

namespace UnitTests.Date.Core;

public class CalendarViewModelColorTests
{
    private static CalendarInfo CreateCalendarInfo(string? color = "#FF5733")
    {
        return new CalendarInfo
        {
            Id = "cal_1",
            AccountId = "acct_1",
            Name = "Work",
            Color = color,
        };
    }

    [Fact]
    public void Color_WithoutOverride_ReturnsProviderColor()
    {
        var vm = new CalendarViewModel(CreateCalendarInfo("#FF5733"), isVisible: true);

        vm.Color.Should().Be("#FF5733");
    }

    [Fact]
    public void Color_WithOverride_ReturnsOverrideColor()
    {
        var vm = new CalendarViewModel(CreateCalendarInfo("#FF5733"), isVisible: true, colorOverride: "#00FF00");

        vm.Color.Should().Be("#00FF00");
    }

    [Fact]
    public void SetColor_UpdatesColorAndRaisesPropertyChanged()
    {
        var vm = new CalendarViewModel(CreateCalendarInfo("#FF5733"), isVisible: true);
        bool colorChanged = false;
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(CalendarViewModel.Color))
            {
                colorChanged = true;
            }
        };

        vm.SetColor("#00FF00");

        vm.Color.Should().Be("#00FF00");
        colorChanged.Should().BeTrue();
    }

    [Fact]
    public void SetColor_Null_RevertsToProviderColor()
    {
        var vm = new CalendarViewModel(CreateCalendarInfo("#FF5733"), isVisible: true, colorOverride: "#00FF00");

        vm.SetColor(null);

        vm.Color.Should().Be("#FF5733");
    }

    [Fact]
    public void SetColor_SameValue_DoesNotFireEvents()
    {
        var vm = new CalendarViewModel(CreateCalendarInfo("#FF5733"), isVisible: true, colorOverride: "#00FF00");
        bool colorChangedFired = false;
        vm.ColorChanged += (_, _) => colorChangedFired = true;

        vm.SetColor("#00FF00"); // Same as current override.

        colorChangedFired.Should().BeFalse();
    }

    [Fact]
    public void PreviewColor_UpdatesColorButDoesNotFireColorChanged()
    {
        var vm = new CalendarViewModel(CreateCalendarInfo("#FF5733"), isVisible: true);
        bool colorChangedFired = false;
        vm.ColorChanged += (_, _) => colorChangedFired = true;

        vm.PreviewColor("#00FF00");

        vm.Color.Should().Be("#00FF00");
        colorChangedFired.Should().BeFalse();
    }

    [Fact]
    public void CommitColor_FiresColorChanged()
    {
        var vm = new CalendarViewModel(CreateCalendarInfo("#FF5733"), isVisible: true);
        vm.PreviewColor("#00FF00");

        bool colorChangedFired = false;
        vm.ColorChanged += (_, _) => colorChangedFired = true;

        vm.CommitColor();

        colorChangedFired.Should().BeTrue();
    }

    [Fact]
    public void PreviewThenCommit_FullWorkflow()
    {
        var vm = new CalendarViewModel(CreateCalendarInfo("#FF5733"), isVisible: true);
        var events = new List<string>();
        vm.PropertyChanged += (_, args) => events.Add($"PropertyChanged:{args.PropertyName}");
        vm.ColorChanged += (_, _) => events.Add("ColorChanged");

        vm.PreviewColor("#AABBCC");
        vm.PreviewColor("#DDEEFF");
        vm.CommitColor();

        vm.Color.Should().Be("#DDEEFF");
        events.Should().ContainInOrder(
            "PropertyChanged:Color",
            "PropertyChanged:Color",
            "ColorChanged");
    }
}
