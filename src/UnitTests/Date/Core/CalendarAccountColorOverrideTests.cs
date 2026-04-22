using FluentAssertions;
using WindowSill.Date.Core.Models;

namespace UnitTests.Date.Core;

public class CalendarAccountColorOverrideTests
{
    private static CalendarAccount CreateAccount(
        Dictionary<string, string>? colorOverrides = null)
    {
        return new CalendarAccount
        {
            Id = "acct_1",
            DisplayName = "Test",
            Email = "test@test.com",
            ProviderType = CalendarProviderType.Outlook,
            CalendarColorOverrides = colorOverrides ?? new Dictionary<string, string>(),
        };
    }

    [Fact]
    public void CalendarColorOverrides_DefaultsToEmpty()
    {
        CalendarAccount account = new()
        {
            Id = "test",
            DisplayName = "Test",
            Email = "test@test.com",
            ProviderType = CalendarProviderType.Google,
        };

        account.CalendarColorOverrides.Should().NotBeNull();
        account.CalendarColorOverrides.Should().BeEmpty();
    }

    [Fact]
    public void WithCalendarColorOverrides_CreatesNewInstanceWithUpdatedOverrides()
    {
        CalendarAccount original = CreateAccount(new() { ["cal_1"] = "#FF0000" });

        var newOverrides = new Dictionary<string, string> { ["cal_2"] = "#00FF00" };
        CalendarAccount updated = original.WithCalendarColorOverrides(newOverrides);

        updated.Should().NotBeSameAs(original);
        updated.CalendarColorOverrides.Should().ContainKey("cal_2").WhoseValue.Should().Be("#00FF00");
        updated.CalendarColorOverrides.Should().NotContainKey("cal_1");

        // Metadata preserved.
        updated.Id.Should().Be("acct_1");
        updated.Email.Should().Be("test@test.com");

        // Original unchanged.
        original.CalendarColorOverrides.Should().ContainKey("cal_1");
    }

    [Fact]
    public void WithAuthData_PreservesCalendarColorOverrides()
    {
        CalendarAccount original = CreateAccount(new() { ["cal_1"] = "#FF0000" });

        CalendarAccount updated = original.WithAuthData(new Dictionary<string, string> { ["token"] = "abc" });

        updated.CalendarColorOverrides.Should().ContainKey("cal_1").WhoseValue.Should().Be("#FF0000");
    }

    [Fact]
    public void WithHiddenCalendarIds_PreservesCalendarColorOverrides()
    {
        CalendarAccount original = CreateAccount(new() { ["cal_1"] = "#FF0000" });

        CalendarAccount updated = original.WithHiddenCalendarIds(["cal_2"]);

        updated.CalendarColorOverrides.Should().ContainKey("cal_1").WhoseValue.Should().Be("#FF0000");
        updated.HiddenCalendarIds.Should().Contain("cal_2");
    }

    [Fact]
    public void WithAuthData_DefensivelyCopiesCollections()
    {
        var overrides = new Dictionary<string, string> { ["cal_1"] = "#FF0000" };
        CalendarAccount original = CreateAccount(overrides);

        CalendarAccount updated = original.WithAuthData(new Dictionary<string, string>());

        // Mutating the copy should not affect original.
        ((Dictionary<string, string>)updated.CalendarColorOverrides)["cal_1"] = "#00FF00";
        original.CalendarColorOverrides["cal_1"].Should().Be("#FF0000");
    }
}
