using FluentAssertions;
using Ical.Net.DataTypes;
using WindowSill.Date.Core.Models;
using WindowSill.Date.Core.Providers.CalDav;
using ICalEvent = Ical.Net.CalendarComponents.CalendarEvent;

namespace UnitTests.Date.Core;

public class CalDavEventMapperTests
{
    private static CalendarInfo CreateCalendar(string? color = "#4285F4")
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
    public void MapVEvent_BasicEvent_MapsCorrectly()
    {
        var vEvent = new ICalEvent
        {
            Uid = "evt_123",
            Summary = "Daily Stand-up",
            Start = new CalDateTime(2026, 4, 22, 10, 0, 0, "UTC"),
            End = new CalDateTime(2026, 4, 22, 10, 30, 0, "UTC"),
        };

        CalendarInfo calendar = CreateCalendar();
        CalendarEvent result = CalDavEventMapper.MapVEvent(vEvent, calendar);

        result.Id.Should().Be("evt_123");
        result.Title.Should().Be("Daily Stand-up");
        result.CalendarId.Should().Be("cal_1");
        result.AccountId.Should().Be("acct_1");
        result.Color.Should().Be("#4285F4");
        result.IsAllDay.Should().BeFalse();
        result.ProviderType.Should().Be(CalendarProviderType.CalDav);
    }

    [Fact]
    public void MapVEvent_AllDayEvent_SetsIsAllDay()
    {
        var vEvent = new ICalEvent
        {
            Uid = "evt_allday",
            Summary = "Holiday",
            Start = new CalDateTime(2026, 4, 22),
            End = new CalDateTime(2026, 4, 23),
        };

        CalendarEvent result = CalDavEventMapper.MapVEvent(vEvent, CreateCalendar());

        result.IsAllDay.Should().BeTrue();
    }

    [Fact]
    public void MapVEvent_NoSummary_DefaultsToNoTitle()
    {
        var vEvent = new ICalEvent
        {
            Uid = "evt_notitle",
            Start = new CalDateTime(2026, 4, 22, 9, 0, 0, "UTC"),
            End = new CalDateTime(2026, 4, 22, 10, 0, 0, "UTC"),
        };

        CalendarEvent result = CalDavEventMapper.MapVEvent(vEvent, CreateCalendar());

        result.Title.Should().Be("No Title");
    }

    [Fact]
    public void MapVEvent_CancelledStatus_MapsToCancelled()
    {
        var vEvent = new ICalEvent
        {
            Uid = "evt_cancelled",
            Summary = "Cancelled Meeting",
            Status = "CANCELLED",
            Start = new CalDateTime(2026, 4, 22, 9, 0, 0, "UTC"),
            End = new CalDateTime(2026, 4, 22, 10, 0, 0, "UTC"),
        };

        CalendarEvent result = CalDavEventMapper.MapVEvent(vEvent, CreateCalendar());

        result.Status.Should().Be(CalendarEventStatus.Cancelled);
    }

    [Fact]
    public void MapVEvent_TransparentEvent_MapsToFree()
    {
        var vEvent = new ICalEvent
        {
            Uid = "evt_free",
            Summary = "Lunch",
            Transparency = "TRANSPARENT",
            Start = new CalDateTime(2026, 4, 22, 12, 0, 0, "UTC"),
            End = new CalDateTime(2026, 4, 22, 13, 0, 0, "UTC"),
        };

        CalendarEvent result = CalDavEventMapper.MapVEvent(vEvent, CreateCalendar());

        result.BusyStatus.Should().Be(BusyStatus.Free);
    }

    [Fact]
    public void MapVEvent_PrivateEvent_MapsIsPrivate()
    {
        var vEvent = new ICalEvent
        {
            Uid = "evt_private",
            Summary = "Private",
            Class = "PRIVATE",
            Start = new CalDateTime(2026, 4, 22, 14, 0, 0, "UTC"),
            End = new CalDateTime(2026, 4, 22, 15, 0, 0, "UTC"),
        };

        CalendarEvent result = CalDavEventMapper.MapVEvent(vEvent, CreateCalendar());

        result.IsPrivate.Should().BeTrue();
    }

    [Fact]
    public void MapVEvent_CustomProviderType_IsPreserved()
    {
        var vEvent = new ICalEvent
        {
            Uid = "evt_icloud",
            Summary = "iCloud Event",
            Start = new CalDateTime(2026, 4, 22, 9, 0, 0, "UTC"),
            End = new CalDateTime(2026, 4, 22, 10, 0, 0, "UTC"),
        };

        CalendarEvent result = CalDavEventMapper.MapVEvent(vEvent, CreateCalendar(), CalendarProviderType.ICloud);

        result.ProviderType.Should().Be(CalendarProviderType.ICloud);
    }

    [Fact]
    public void MapVEvent_WithZoomLink_DetectsVideoCall()
    {
        var vEvent = new ICalEvent
        {
            Uid = "evt_zoom",
            Summary = "Zoom Call",
            Description = "Join: https://zoom.us/j/123456789",
            Start = new CalDateTime(2026, 4, 22, 16, 0, 0, "UTC"),
            End = new CalDateTime(2026, 4, 22, 17, 0, 0, "UTC"),
        };

        CalendarEvent result = CalDavEventMapper.MapVEvent(vEvent, CreateCalendar());

        result.VideoCall.Should().NotBeNull();
        result.VideoCall!.Provider.Should().Be(VideoCallProvider.Zoom);
    }

    // ── Response status tests ──

    [Theory]
    [InlineData("ACCEPTED", AttendeeResponseStatus.Accepted)]
    [InlineData("TENTATIVE", AttendeeResponseStatus.Tentative)]
    [InlineData("DECLINED", AttendeeResponseStatus.Declined)]
    [InlineData("NEEDS-ACTION", AttendeeResponseStatus.NotResponded)]
    [InlineData(null, AttendeeResponseStatus.NotResponded)]
    public void MapPartStat_MapsCorrectly(string? partStat, AttendeeResponseStatus expected)
    {
        CalDavEventMapper.MapPartStat(partStat).Should().Be(expected);
    }

    [Fact]
    public void MapVEvent_UserIsAttendee_ResolvesTheirPartStat()
    {
        var vEvent = new ICalEvent
        {
            Uid = "evt_meeting",
            Summary = "Team Sync",
            Start = new CalDateTime(2026, 4, 22, 10, 0, 0, "UTC"),
            End = new CalDateTime(2026, 4, 22, 11, 0, 0, "UTC"),
        };
        vEvent.Attendees.Add(new Ical.Net.DataTypes.Attendee("mailto:alice@example.com")
            { ParticipationStatus = "ACCEPTED" });
        vEvent.Attendees.Add(new Ical.Net.DataTypes.Attendee("mailto:bob@example.com")
            { ParticipationStatus = "TENTATIVE" });

        CalendarEvent result = CalDavEventMapper.MapVEvent(
            vEvent, CreateCalendar(), accountEmail: "bob@example.com");

        result.ResponseStatus.Should().Be(AttendeeResponseStatus.Tentative);
    }

    [Fact]
    public void MapVEvent_UserIsOrganizer_ReturnsAccepted()
    {
        var vEvent = new ICalEvent
        {
            Uid = "evt_organized",
            Summary = "My Meeting",
            Start = new CalDateTime(2026, 4, 22, 14, 0, 0, "UTC"),
            End = new CalDateTime(2026, 4, 22, 15, 0, 0, "UTC"),
            Organizer = new Ical.Net.DataTypes.Organizer("mailto:me@example.com"),
        };

        CalendarEvent result = CalDavEventMapper.MapVEvent(
            vEvent, CreateCalendar(), accountEmail: "me@example.com");

        result.ResponseStatus.Should().Be(AttendeeResponseStatus.Accepted);
    }

    [Fact]
    public void MapVEvent_UserNotInAttendees_PersonalEvent_ReturnsAccepted()
    {
        var vEvent = new ICalEvent
        {
            Uid = "evt_personal",
            Summary = "Lunch",
            Start = new CalDateTime(2026, 4, 22, 12, 0, 0, "UTC"),
            End = new CalDateTime(2026, 4, 22, 13, 0, 0, "UTC"),
        };

        CalendarEvent result = CalDavEventMapper.MapVEvent(
            vEvent, CreateCalendar(), accountEmail: "me@example.com");

        result.ResponseStatus.Should().Be(AttendeeResponseStatus.Accepted);
    }

    [Fact]
    public void MapVEvent_NoAccountEmail_ReturnsNotResponded()
    {
        var vEvent = new ICalEvent
        {
            Uid = "evt_noemail",
            Summary = "Unknown",
            Start = new CalDateTime(2026, 4, 22, 9, 0, 0, "UTC"),
            End = new CalDateTime(2026, 4, 22, 10, 0, 0, "UTC"),
        };

        CalendarEvent result = CalDavEventMapper.MapVEvent(vEvent, CreateCalendar());

        result.ResponseStatus.Should().Be(AttendeeResponseStatus.NotResponded);
    }

    [Fact]
    public void MapVEvent_AttendeePartStats_MappedIndividually()
    {
        var vEvent = new ICalEvent
        {
            Uid = "evt_multi",
            Summary = "Group Call",
            Start = new CalDateTime(2026, 4, 22, 15, 0, 0, "UTC"),
            End = new CalDateTime(2026, 4, 22, 16, 0, 0, "UTC"),
        };
        vEvent.Attendees.Add(new Ical.Net.DataTypes.Attendee("mailto:alice@example.com")
            { ParticipationStatus = "ACCEPTED" });
        vEvent.Attendees.Add(new Ical.Net.DataTypes.Attendee("mailto:bob@example.com")
            { ParticipationStatus = "DECLINED" });

        CalendarEvent result = CalDavEventMapper.MapVEvent(vEvent, CreateCalendar());

        result.Attendees.Should().HaveCount(2);
        result.Attendees[0].ResponseStatus.Should().Be(AttendeeResponseStatus.Accepted);
        result.Attendees[1].ResponseStatus.Should().Be(AttendeeResponseStatus.Declined);
    }
}
