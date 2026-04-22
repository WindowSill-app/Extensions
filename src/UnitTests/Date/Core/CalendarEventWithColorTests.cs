using FluentAssertions;
using WindowSill.Date.Core.Models;

namespace UnitTests.Date.Core;

public class CalendarEventWithColorTests
{
    private static CalendarEvent CreateEvent(string? color = "#FF5733")
    {
        return new CalendarEvent
        {
            Id = "evt_1",
            CalendarId = "cal_1",
            AccountId = "acct_1",
            Title = "Stand-up",
            StartTime = DateTimeOffset.Now,
            EndTime = DateTimeOffset.Now.AddHours(1),
            ProviderType = CalendarProviderType.Google,
            Color = color,
        };
    }

    [Fact]
    public void WithColor_CreatesNewInstanceWithDifferentColor()
    {
        CalendarEvent original = CreateEvent("#FF5733");

        CalendarEvent updated = original.WithColor("#00FF00");

        updated.Color.Should().Be("#00FF00");
        original.Color.Should().Be("#FF5733");
    }

    [Fact]
    public void WithColor_PreservesAllOtherProperties()
    {
        CalendarEvent original = CreateEvent();

        CalendarEvent updated = original.WithColor("#000000");

        updated.Id.Should().Be(original.Id);
        updated.CalendarId.Should().Be(original.CalendarId);
        updated.AccountId.Should().Be(original.AccountId);
        updated.Title.Should().Be(original.Title);
        updated.StartTime.Should().Be(original.StartTime);
        updated.EndTime.Should().Be(original.EndTime);
        updated.ProviderType.Should().Be(original.ProviderType);
    }

    [Fact]
    public void WithColor_Null_ClearsColor()
    {
        CalendarEvent original = CreateEvent("#FF5733");

        CalendarEvent updated = original.WithColor(null);

        updated.Color.Should().BeNull();
    }
}
