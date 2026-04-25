using FluentAssertions;
using WindowSill.Date.Core.Models;

namespace UnitTests.Date.Core;

public class CalendarAccountTests
{
    [Fact]
    public void WithAuthData_CreatesNewInstanceWithUpdatedAuth()
    {
        CalendarAccount original = new()
        {
            Id = "test_1",
            DisplayName = "Test",
            Email = "test@test.com",
            ProviderType = CalendarProviderType.Outlook,
            AuthData = new Dictionary<string, string> { ["old_key"] = "old_value" },
        };

        IReadOnlyDictionary<string, string> newAuth = new Dictionary<string, string> { ["new_key"] = "new_value" };
        CalendarAccount updated = original.WithAuthData(newAuth);

        // New instance with new auth data.
        updated.Should().NotBeSameAs(original);
        updated.AuthData.Should().ContainKey("new_key").WhoseValue.Should().Be("new_value");
        updated.AuthData.Should().NotContainKey("old_key");

        // Metadata preserved.
        updated.Id.Should().Be("test_1");
        updated.DisplayName.Should().Be("Test");
        updated.Email.Should().Be("test@test.com");
        updated.ProviderType.Should().Be(CalendarProviderType.Outlook);

        // Original unchanged.
        original.AuthData.Should().ContainKey("old_key");
        original.AuthData.Should().NotContainKey("new_key");
    }

    [Fact]
    public void AuthData_DefaultsToEmptyDictionary()
    {
        CalendarAccount account = new()
        {
            Id = "test",
            DisplayName = "Test",
            Email = "test@test.com",
            ProviderType = CalendarProviderType.Google,
        };

        account.AuthData.Should().NotBeNull();
        account.AuthData.Should().BeEmpty();
    }

    [Fact]
    public void WithHiddenCalendarIds_CreatesNewInstanceWithUpdatedHiddenCalendars()
    {
        CalendarAccount original = new()
        {
            Id = "test_1",
            DisplayName = "Test",
            Email = "test@test.com",
            ProviderType = CalendarProviderType.Outlook,
            HiddenCalendarIds = ["cal_1"],
        };

        HashSet<string> newHidden = ["cal_2", "cal_3"];
        CalendarAccount updated = original.WithHiddenCalendarIds(newHidden);

        updated.Should().NotBeSameAs(original);
        updated.HiddenCalendarIds.Should().BeEquivalentTo(["cal_2", "cal_3"]);
        updated.Id.Should().Be("test_1");
        updated.AuthData.Should().BeEmpty();

        original.HiddenCalendarIds.Should().BeEquivalentTo(["cal_1"]);
    }

    [Fact]
    public void HiddenCalendarIds_DefaultsToEmpty()
    {
        CalendarAccount account = new()
        {
            Id = "test",
            DisplayName = "Test",
            Email = "test@test.com",
            ProviderType = CalendarProviderType.Google,
        };

        account.HiddenCalendarIds.Should().NotBeNull();
        account.HiddenCalendarIds.Should().BeEmpty();
    }
}
