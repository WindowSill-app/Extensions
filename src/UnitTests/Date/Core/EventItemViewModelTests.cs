using FluentAssertions;
using WindowSill.Date.Core.Models;
using WindowSill.Date.ViewModels;

namespace UnitTests.Date.Core;

public class EventItemViewModelTests
{
    private static CalendarEvent CreateTimedEvent(
        DateTimeOffset start,
        DateTimeOffset end,
        string title = "Test Meeting",
        string? color = "#FF5733",
        VideoCallInfo? videoCall = null,
        Uri? webLink = null,
        CalendarEventStatus status = CalendarEventStatus.Confirmed,
        AttendeeResponseStatus responseStatus = AttendeeResponseStatus.Accepted)
    {
        return new CalendarEvent
        {
            Id = Guid.NewGuid().ToString(),
            CalendarId = "cal_1",
            AccountId = "acc_1",
            Title = title,
            StartTime = start,
            EndTime = end,
            IsAllDay = false,
            Color = color,
            VideoCall = videoCall,
            WebLink = webLink,
            Status = status,
            ResponseStatus = responseStatus,
            ProviderType = CalendarProviderType.Outlook,
        };
    }

    private static CalendarEvent CreateAllDayEvent(string title = "Holiday")
    {
        return new CalendarEvent
        {
            Id = Guid.NewGuid().ToString(),
            CalendarId = "cal_1",
            AccountId = "acc_1",
            Title = title,
            StartTime = DateTimeOffset.Now.Date,
            EndTime = DateTimeOffset.Now.Date.AddDays(1),
            IsAllDay = true,
            ProviderType = CalendarProviderType.Google,
        };
    }

    #region TimeRangeText

    [Fact]
    public void TimeRangeText_TimedEvent_ContainsStartAndEndTime()
    {
        DateTimeOffset start = new(2026, 4, 19, 9, 0, 0, TimeSpan.FromHours(-7));
        DateTimeOffset end = new(2026, 4, 19, 10, 30, 0, TimeSpan.FromHours(-7));
        CalendarEvent evt = CreateTimedEvent(start, end);
        var vm = new EventItemViewModel(evt);

        vm.TimeRangeText.Should().Contain("–");
        vm.TimeRangeText.Should().NotBeEmpty();
    }

    #endregion

    #region IsNow

    [Fact]
    public void IsNow_InProgressEvent_ReturnsTrue()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        CalendarEvent evt = CreateTimedEvent(now.AddMinutes(-30), now.AddMinutes(30));
        var vm = new EventItemViewModel(evt);

        vm.IsNow.Should().BeTrue();
    }

    [Fact]
    public void IsNow_FutureEvent_ReturnsFalse()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        CalendarEvent evt = CreateTimedEvent(now.AddHours(1), now.AddHours(2));
        var vm = new EventItemViewModel(evt);

        vm.IsNow.Should().BeFalse();
    }

    [Fact]
    public void IsNow_PastEvent_ReturnsFalse()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        CalendarEvent evt = CreateTimedEvent(now.AddHours(-2), now.AddHours(-1));
        var vm = new EventItemViewModel(evt);

        vm.IsNow.Should().BeFalse();
    }

    [Fact]
    public void IsNow_AtExactEndTime_ReturnsFalse()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        CalendarEvent evt = CreateTimedEvent(now.AddHours(-1), now);
        var vm = new EventItemViewModel(evt);

        vm.IsNow.Should().BeFalse("end time is exclusive");
    }

    [Fact]
    public void IsNow_AllDayEvent_ReturnsFalse()
    {
        CalendarEvent evt = CreateAllDayEvent();
        var vm = new EventItemViewModel(evt);

        vm.IsNow.Should().BeFalse("all-day events don't show as 'Now'");
    }

    #endregion

    #region IsPast

    [Fact]
    public void IsPast_PastEvent_ReturnsTrue()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        CalendarEvent evt = CreateTimedEvent(now.AddHours(-3), now.AddHours(-1));
        var vm = new EventItemViewModel(evt);

        vm.IsPast.Should().BeTrue();
    }

    [Fact]
    public void IsPast_FutureEvent_ReturnsFalse()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        CalendarEvent evt = CreateTimedEvent(now.AddHours(1), now.AddHours(2));
        var vm = new EventItemViewModel(evt);

        vm.IsPast.Should().BeFalse();
    }

    [Fact]
    public void IsPast_InProgressEvent_ReturnsFalse()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        CalendarEvent evt = CreateTimedEvent(now.AddMinutes(-30), now.AddMinutes(30));
        var vm = new EventItemViewModel(evt);

        vm.IsPast.Should().BeFalse();
    }

    #endregion

    #region Status flags

    [Fact]
    public void IsCancelled_CancelledEvent_ReturnsTrue()
    {
        CalendarEvent evt = CreateTimedEvent(
            DateTimeOffset.Now.AddHours(1),
            DateTimeOffset.Now.AddHours(2),
            status: CalendarEventStatus.Cancelled);
        var vm = new EventItemViewModel(evt);

        vm.IsCancelled.Should().BeTrue();
    }

    [Fact]
    public void IsCancelled_ConfirmedEvent_ReturnsFalse()
    {
        CalendarEvent evt = CreateTimedEvent(
            DateTimeOffset.Now.AddHours(1),
            DateTimeOffset.Now.AddHours(2));
        var vm = new EventItemViewModel(evt);

        vm.IsCancelled.Should().BeFalse();
    }

    [Fact]
    public void IsDeclined_DeclinedResponse_ReturnsTrue()
    {
        CalendarEvent evt = CreateTimedEvent(
            DateTimeOffset.Now.AddHours(1),
            DateTimeOffset.Now.AddHours(2),
            responseStatus: AttendeeResponseStatus.Declined);
        var vm = new EventItemViewModel(evt);

        vm.IsDeclined.Should().BeTrue();
    }

    #endregion

    #region Video call

    [Fact]
    public void HasVideoCall_WithVideoCallInfo_ReturnsTrue()
    {
        var videoCall = new VideoCallInfo(new Uri("https://teams.microsoft.com/l/meetup-join/123"), VideoCallProvider.MicrosoftTeams);
        CalendarEvent evt = CreateTimedEvent(
            DateTimeOffset.Now.AddHours(1),
            DateTimeOffset.Now.AddHours(2),
            videoCall: videoCall);
        var vm = new EventItemViewModel(evt);

        vm.HasVideoCall.Should().BeTrue();
        vm.VideoCallUrl.Should().NotBeNull();
        vm.VideoCallUrl!.ToString().Should().StartWith("https://teams.microsoft.com");
    }

    [Fact]
    public void HasVideoCall_NoVideoCallInfo_ReturnsFalse()
    {
        CalendarEvent evt = CreateTimedEvent(
            DateTimeOffset.Now.AddHours(1),
            DateTimeOffset.Now.AddHours(2));
        var vm = new EventItemViewModel(evt);

        vm.HasVideoCall.Should().BeFalse();
        vm.VideoCallUrl.Should().BeNull();
    }

    #endregion

    #region WebLink

    [Fact]
    public void HasWebLink_WithUri_ReturnsTrue()
    {
        CalendarEvent evt = CreateTimedEvent(
            DateTimeOffset.Now.AddHours(1),
            DateTimeOffset.Now.AddHours(2),
            webLink: new Uri("https://outlook.office365.com/calendar/item/123"));
        var vm = new EventItemViewModel(evt);

        vm.HasWebLink.Should().BeTrue();
        vm.WebLink.Should().NotBeNull();
    }

    [Fact]
    public void HasWebLink_NoUri_ReturnsFalse()
    {
        CalendarEvent evt = CreateTimedEvent(
            DateTimeOffset.Now.AddHours(1),
            DateTimeOffset.Now.AddHours(2));
        var vm = new EventItemViewModel(evt);

        vm.HasWebLink.Should().BeFalse();
    }

    #endregion

    #region Properties

    [Fact]
    public void Title_ReflectsEvent()
    {
        CalendarEvent evt = CreateTimedEvent(
            DateTimeOffset.Now.AddHours(1),
            DateTimeOffset.Now.AddHours(2),
            title: "Team Sync");
        var vm = new EventItemViewModel(evt);

        vm.Title.Should().Be("Team Sync");
    }

    [Fact]
    public void Color_ReflectsEvent()
    {
        CalendarEvent evt = CreateTimedEvent(
            DateTimeOffset.Now.AddHours(1),
            DateTimeOffset.Now.AddHours(2),
            color: "#3498DB");
        var vm = new EventItemViewModel(evt);

        vm.Color.Should().Be("#3498DB");
    }

    [Fact]
    public void IsAllDay_ForAllDayEvent_ReturnsTrue()
    {
        CalendarEvent evt = CreateAllDayEvent();
        var vm = new EventItemViewModel(evt);

        vm.IsAllDay.Should().BeTrue();
    }

    #endregion
}
