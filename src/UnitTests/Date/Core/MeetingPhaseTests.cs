using FluentAssertions;
using UnitTests.Date.Core.Fakes;
using WindowSill.Date.Core.Models;
using WindowSill.Date.ViewModels;

namespace UnitTests.Date.Core;

public class MeetingPhaseTests
{
    public MeetingPhaseTests()
    {
        LocalizerSetup.EnsureInitialized();
    }

    #region ComputePhase

    [Fact]
    public void ComputePhase_MoreThan5MinBefore_ReturnsNormal()
    {
        TimeSpan timeUntilStart = TimeSpan.FromMinutes(22);
        TimeSpan timeUntilEnd = TimeSpan.FromMinutes(82);

        MeetingPhase phase = MeetingSillItemViewModel.ComputePhase(timeUntilStart, timeUntilEnd);

        phase.Should().Be(MeetingPhase.Normal);
    }

    [Fact]
    public void ComputePhase_Exactly5MinBefore_ReturnsUrgent()
    {
        TimeSpan timeUntilStart = TimeSpan.FromMinutes(5);
        TimeSpan timeUntilEnd = TimeSpan.FromMinutes(65);

        MeetingPhase phase = MeetingSillItemViewModel.ComputePhase(timeUntilStart, timeUntilEnd);

        phase.Should().Be(MeetingPhase.Urgent);
    }

    [Fact]
    public void ComputePhase_4Min59Sec_ReturnsUrgent()
    {
        TimeSpan timeUntilStart = TimeSpan.FromSeconds(4 * 60 + 59);
        TimeSpan timeUntilEnd = TimeSpan.FromMinutes(65);

        MeetingPhase phase = MeetingSillItemViewModel.ComputePhase(timeUntilStart, timeUntilEnd);

        phase.Should().Be(MeetingPhase.Urgent);
    }

    [Fact]
    public void ComputePhase_30SecBefore_ReturnsFlashing()
    {
        TimeSpan timeUntilStart = TimeSpan.FromSeconds(30);
        TimeSpan timeUntilEnd = TimeSpan.FromMinutes(60);

        MeetingPhase phase = MeetingSillItemViewModel.ComputePhase(timeUntilStart, timeUntilEnd);

        phase.Should().Be(MeetingPhase.Flashing);
    }

    [Fact]
    public void ComputePhase_15SecBefore_ReturnsFlashing()
    {
        TimeSpan timeUntilStart = TimeSpan.FromSeconds(15);
        TimeSpan timeUntilEnd = TimeSpan.FromMinutes(60);

        MeetingPhase phase = MeetingSillItemViewModel.ComputePhase(timeUntilStart, timeUntilEnd);

        phase.Should().Be(MeetingPhase.Flashing);
    }

    [Fact]
    public void ComputePhase_JustStarted_ReturnsLive()
    {
        TimeSpan timeUntilStart = TimeSpan.FromSeconds(-10); // 10 sec after start
        TimeSpan timeUntilEnd = TimeSpan.FromMinutes(50);

        MeetingPhase phase = MeetingSillItemViewModel.ComputePhase(timeUntilStart, timeUntilEnd);

        phase.Should().Be(MeetingPhase.Live);
    }

    [Fact]
    public void ComputePhase_59SecAfterStart_StillLive()
    {
        TimeSpan timeUntilStart = TimeSpan.FromSeconds(-59);
        TimeSpan timeUntilEnd = TimeSpan.FromMinutes(59);

        MeetingPhase phase = MeetingSillItemViewModel.ComputePhase(timeUntilStart, timeUntilEnd);

        phase.Should().Be(MeetingPhase.Live);
    }

    [Fact]
    public void ComputePhase_1MinAfterStart_ReturnsElapsed()
    {
        TimeSpan timeUntilStart = TimeSpan.FromMinutes(-1);
        TimeSpan timeUntilEnd = TimeSpan.FromMinutes(59);

        MeetingPhase phase = MeetingSillItemViewModel.ComputePhase(timeUntilStart, timeUntilEnd);

        phase.Should().Be(MeetingPhase.Elapsed);
    }

    [Fact]
    public void ComputePhase_30MinAfterStart_ReturnsElapsed()
    {
        TimeSpan timeUntilStart = TimeSpan.FromMinutes(-30);
        TimeSpan timeUntilEnd = TimeSpan.FromMinutes(30);

        MeetingPhase phase = MeetingSillItemViewModel.ComputePhase(timeUntilStart, timeUntilEnd);

        phase.Should().Be(MeetingPhase.Elapsed);
    }

    [Fact]
    public void ComputePhase_MeetingEnded_ReturnsEnded()
    {
        TimeSpan timeUntilStart = TimeSpan.FromMinutes(-70);
        TimeSpan timeUntilEnd = TimeSpan.FromMinutes(-10);

        MeetingPhase phase = MeetingSillItemViewModel.ComputePhase(timeUntilStart, timeUntilEnd);

        phase.Should().Be(MeetingPhase.Ended);
    }

    [Fact]
    public void ComputePhase_ExactlyAtEnd_ReturnsEnded()
    {
        TimeSpan timeUntilStart = TimeSpan.FromMinutes(-60);
        TimeSpan timeUntilEnd = TimeSpan.Zero;

        MeetingPhase phase = MeetingSillItemViewModel.ComputePhase(timeUntilStart, timeUntilEnd);

        phase.Should().Be(MeetingPhase.Ended);
    }

    #endregion

    #region Phase boundaries — boundary value testing

    [Fact]
    public void ComputePhase_5Min1SecBefore_ReturnsNormal()
    {
        TimeSpan timeUntilStart = TimeSpan.FromSeconds(5 * 60 + 1);
        TimeSpan timeUntilEnd = TimeSpan.FromMinutes(65);

        MeetingPhase phase = MeetingSillItemViewModel.ComputePhase(timeUntilStart, timeUntilEnd);

        phase.Should().Be(MeetingPhase.Normal);
    }

    [Fact]
    public void ComputePhase_31SecBefore_ReturnsUrgent()
    {
        TimeSpan timeUntilStart = TimeSpan.FromSeconds(31);
        TimeSpan timeUntilEnd = TimeSpan.FromMinutes(60);

        MeetingPhase phase = MeetingSillItemViewModel.ComputePhase(timeUntilStart, timeUntilEnd);

        phase.Should().Be(MeetingPhase.Urgent);
    }

    [Fact]
    public void ComputePhase_ExactlyAtStart_ReturnsLive()
    {
        TimeSpan timeUntilStart = TimeSpan.Zero;
        TimeSpan timeUntilEnd = TimeSpan.FromMinutes(60);

        MeetingPhase phase = MeetingSillItemViewModel.ComputePhase(timeUntilStart, timeUntilEnd);

        phase.Should().Be(MeetingPhase.Live);
    }

    #endregion

    #region MeetingKey

    [Fact]
    public void MeetingKey_FromEvent_UsesIdAccountAndStartTime()
    {
        var evt = new CalendarEvent
        {
            Id = "event-123",
            CalendarId = "cal-1",
            AccountId = "acc-42",
            Title = "Standup",
            StartTime = new DateTimeOffset(2026, 4, 20, 10, 0, 0, TimeSpan.FromHours(-7)),
            EndTime = new DateTimeOffset(2026, 4, 20, 10, 30, 0, TimeSpan.FromHours(-7)),
            ProviderType = CalendarProviderType.Outlook,
        };

        MeetingKey key = MeetingKey.FromEvent(evt);

        key.EventId.Should().Be("event-123");
        key.AccountId.Should().Be("acc-42");
        key.StartTime.Should().Be(evt.StartTime);
    }

    [Fact]
    public void MeetingKey_Equality_MatchesSameEvent()
    {
        var start = new DateTimeOffset(2026, 4, 20, 10, 0, 0, TimeSpan.Zero);
        var key1 = new MeetingKey("ev1", "acc1", start);
        var key2 = new MeetingKey("ev1", "acc1", start);

        key1.Should().Be(key2);
    }

    [Fact]
    public void MeetingKey_Inequality_DifferentId()
    {
        var start = new DateTimeOffset(2026, 4, 20, 10, 0, 0, TimeSpan.Zero);
        var key1 = new MeetingKey("ev1", "acc1", start);
        var key2 = new MeetingKey("ev2", "acc1", start);

        key1.Should().NotBe(key2);
    }

    #endregion

    #region ComputePhase — Departure (physical meetings)

    [Fact]
    public void ComputePhase_PhysicalMeeting_MoreThan5MinBeforeDeparture_ReturnsNormal()
    {
        TimeSpan timeUntilStart = TimeSpan.FromMinutes(40); // meeting in 40 min
        TimeSpan timeUntilEnd = TimeSpan.FromMinutes(100);
        TimeSpan timeUntilDeparture = TimeSpan.FromMinutes(15); // departure in 15 min

        MeetingPhase phase = MeetingSillItemViewModel.ComputePhase(timeUntilStart, timeUntilEnd, timeUntilDeparture);

        phase.Should().Be(MeetingPhase.Normal);
    }

    [Fact]
    public void ComputePhase_PhysicalMeeting_LessThan5MinBeforeDeparture_ReturnsUrgent()
    {
        TimeSpan timeUntilStart = TimeSpan.FromMinutes(28);
        TimeSpan timeUntilEnd = TimeSpan.FromMinutes(88);
        TimeSpan timeUntilDeparture = TimeSpan.FromMinutes(3); // depart in 3 min

        MeetingPhase phase = MeetingSillItemViewModel.ComputePhase(timeUntilStart, timeUntilEnd, timeUntilDeparture);

        phase.Should().Be(MeetingPhase.Urgent);
    }

    [Fact]
    public void ComputePhase_PhysicalMeeting_30SecBeforeDeparture_ReturnsFlashing()
    {
        TimeSpan timeUntilStart = TimeSpan.FromMinutes(25);
        TimeSpan timeUntilEnd = TimeSpan.FromMinutes(85);
        TimeSpan timeUntilDeparture = TimeSpan.FromSeconds(20);

        MeetingPhase phase = MeetingSillItemViewModel.ComputePhase(timeUntilStart, timeUntilEnd, timeUntilDeparture);

        phase.Should().Be(MeetingPhase.Flashing);
    }

    [Fact]
    public void ComputePhase_PhysicalMeeting_DepartureTimeReached_ReturnsDeparture()
    {
        TimeSpan timeUntilStart = TimeSpan.FromMinutes(25);
        TimeSpan timeUntilEnd = TimeSpan.FromMinutes(85);
        TimeSpan timeUntilDeparture = TimeSpan.Zero;

        MeetingPhase phase = MeetingSillItemViewModel.ComputePhase(timeUntilStart, timeUntilEnd, timeUntilDeparture);

        phase.Should().Be(MeetingPhase.Departure);
    }

    [Fact]
    public void ComputePhase_PhysicalMeeting_PastDeparture_StillDeparture()
    {
        TimeSpan timeUntilStart = TimeSpan.FromMinutes(10); // still 10 min to meeting
        TimeSpan timeUntilEnd = TimeSpan.FromMinutes(70);
        TimeSpan timeUntilDeparture = TimeSpan.FromMinutes(-15); // departed 15 min ago

        MeetingPhase phase = MeetingSillItemViewModel.ComputePhase(timeUntilStart, timeUntilEnd, timeUntilDeparture);

        phase.Should().Be(MeetingPhase.Departure);
    }

    [Fact]
    public void ComputePhase_PhysicalMeeting_MeetingStarted_ReturnsLive()
    {
        TimeSpan timeUntilStart = TimeSpan.FromSeconds(-10);
        TimeSpan timeUntilEnd = TimeSpan.FromMinutes(50);
        TimeSpan timeUntilDeparture = TimeSpan.FromMinutes(-30);

        MeetingPhase phase = MeetingSillItemViewModel.ComputePhase(timeUntilStart, timeUntilEnd, timeUntilDeparture);

        phase.Should().Be(MeetingPhase.Live, "meeting start overrides departure phase");
    }

    [Fact]
    public void ComputePhase_VirtualMeeting_NoDeparture_IgnoresDepartureLogic()
    {
        TimeSpan timeUntilStart = TimeSpan.FromMinutes(3);
        TimeSpan timeUntilEnd = TimeSpan.FromMinutes(63);

        MeetingPhase phase = MeetingSillItemViewModel.ComputePhase(timeUntilStart, timeUntilEnd, timeUntilDeparture: null);

        phase.Should().Be(MeetingPhase.Urgent, "virtual meeting uses meeting-start-relative urgency");
    }

    [Fact]
    public void UpdateCountdown_DepartureBuffer_ShiftsDepartureEarlier()
    {
        // Meeting in 45 min, travel = 25 min → departure at 20 min.
        // With 10 min buffer → departure at 30 min, which is > 5 min away → Normal.
        var vm = CreateMeetingVm(
            start: DateTimeOffset.Now.AddMinutes(45),
            end: DateTimeOffset.Now.AddMinutes(105),
            location: "123 Main St");
        vm.TravelTimeEstimate = TravelTimeEstimateResult.FromProvider(
            TimeSpan.FromMinutes(25), 20000, "ORS");

        vm.UpdateCountdown(DateTimeOffset.Now, showJoinButton: false, departureBufferMinutes: 10);

        // departure = 45 - 25 - 10 = 10 min from now → Urgent (< 5 min? No, 10 min → Normal)
        vm.Phase.Should().Be(MeetingPhase.Normal);
    }

    [Fact]
    public void UpdateCountdown_DepartureBuffer_MakesDepartureUrgent()
    {
        // Meeting in 38 min, travel = 30 min → departure at 8 min.
        // With 5 min buffer → departure at 3 min → Urgent.
        var vm = CreateMeetingVm(
            start: DateTimeOffset.Now.AddMinutes(38),
            end: DateTimeOffset.Now.AddMinutes(98),
            location: "123 Main St");
        vm.TravelTimeEstimate = TravelTimeEstimateResult.FromProvider(
            TimeSpan.FromMinutes(30), 25000, "ORS");

        vm.UpdateCountdown(DateTimeOffset.Now, showJoinButton: false, departureBufferMinutes: 5);

        // departure = 38 - 30 - 5 = 3 min → Urgent
        vm.Phase.Should().Be(MeetingPhase.Urgent);
    }

    #endregion

    #region TravelTimeText

    [Fact]
    public void TravelTimeText_NoEstimate_ReturnsNull()
    {
        var vm = CreateMeetingVm();

        vm.TravelTimeText.Should().BeNull();
        vm.HasTravelTime.Should().BeFalse();
    }

    [Fact]
    public void TravelTimeText_SuccessfulEstimate_ReturnsFormattedText()
    {
        var vm = CreateMeetingVm(location: "123 Main St");
        vm.TravelTimeEstimate = TravelTimeEstimateResult.FromProvider(
            TimeSpan.FromMinutes(25), 15000, "ORS");

        vm.HasTravelTime.Should().BeTrue();
        vm.TravelTimeText.Should().Be("~25 min travel");
    }

    [Fact]
    public void TravelTimeText_FallbackEstimate_ReturnsFormattedText()
    {
        var vm = CreateMeetingVm(location: "123 Main St");
        vm.TravelTimeEstimate = TravelTimeEstimateResult.FromFallback(
            TimeSpan.FromMinutes(30));

        vm.HasTravelTime.Should().BeTrue();
        vm.TravelTimeText.Should().Be("~30 min travel");
    }

    [Fact]
    public void TravelTimeText_FailedEstimate_ReturnsNull()
    {
        var vm = CreateMeetingVm(location: "123 Main St");
        vm.TravelTimeEstimate = TravelTimeEstimateResult.Failed(
            TravelTimeFailureReason.InvalidMeetingAddress);

        vm.HasTravelTime.Should().BeFalse();
        vm.TravelTimeText.Should().BeNull();
    }

    [Fact]
    public void TravelTimeText_RoundsUpMinutes()
    {
        var vm = CreateMeetingVm(location: "123 Main St");
        vm.TravelTimeEstimate = TravelTimeEstimateResult.FromProvider(
            TimeSpan.FromMinutes(18.3), 10000, "ORS");

        vm.TravelTimeText.Should().Be("~19 min travel");
    }

    #endregion

    #region Edge-triggered side effects

    [Fact]
    public void UpdateCountdown_FlashRequestedFiresOnceOnTransitionToFlashing()
    {
        var vm = CreateMeetingVm(
            start: DateTimeOffset.Now.AddSeconds(25),
            end: DateTimeOffset.Now.AddHours(1));

        int flashCount = 0;
        vm.FlashRequested += () => flashCount++;

        // First call: should be Flashing → fires flash.
        vm.UpdateCountdown(DateTimeOffset.Now, showJoinButton: true);
        vm.Phase.Should().Be(MeetingPhase.Flashing);
        flashCount.Should().Be(1);

        // Second call: still Flashing → no additional fire.
        vm.UpdateCountdown(DateTimeOffset.Now, showJoinButton: true);
        flashCount.Should().Be(1, "flash should only fire once");
    }

    [Fact]
    public void UpdateCountdown_NotificationRequestedFiresOnceOnTransitionToLive()
    {
        var vm = CreateMeetingVm(
            start: DateTimeOffset.Now.AddSeconds(-5),
            end: DateTimeOffset.Now.AddHours(1));

        int notifCount = 0;
        vm.NotificationRequested += () => notifCount++;

        vm.UpdateCountdown(DateTimeOffset.Now, showJoinButton: true);
        vm.Phase.Should().Be(MeetingPhase.Live);
        notifCount.Should().Be(1);

        vm.UpdateCountdown(DateTimeOffset.Now, showJoinButton: true);
        notifCount.Should().Be(1, "notification should fire only once");
    }

    [Fact]
    public void UpdateCountdown_IsJoinVisible_TrueWhenUrgentWithVideoCall()
    {
        var vm = CreateMeetingVm(
            start: DateTimeOffset.Now.AddMinutes(3),
            end: DateTimeOffset.Now.AddHours(1),
            hasVideoCall: true);

        vm.UpdateCountdown(DateTimeOffset.Now, showJoinButton: true);

        vm.Phase.Should().Be(MeetingPhase.Urgent);
        vm.IsJoinVisible.Should().BeTrue();
    }

    [Fact]
    public void UpdateCountdown_IsJoinVisible_FalseWhenNormalPhase()
    {
        var vm = CreateMeetingVm(
            start: DateTimeOffset.Now.AddMinutes(10),
            end: DateTimeOffset.Now.AddHours(1),
            hasVideoCall: true);

        vm.UpdateCountdown(DateTimeOffset.Now, showJoinButton: true);

        vm.Phase.Should().Be(MeetingPhase.Normal);
        vm.IsJoinVisible.Should().BeFalse();
    }

    [Fact]
    public void UpdateCountdown_IsJoinVisible_FalseWhenSettingDisabled()
    {
        var vm = CreateMeetingVm(
            start: DateTimeOffset.Now.AddMinutes(3),
            end: DateTimeOffset.Now.AddHours(1),
            hasVideoCall: true);

        vm.UpdateCountdown(DateTimeOffset.Now, showJoinButton: false);

        vm.Phase.Should().Be(MeetingPhase.Urgent);
        vm.IsJoinVisible.Should().BeFalse();
    }

    [Fact]
    public void UpdateCountdown_IsUrgent_TrueForUrgentFlashingLive()
    {
        // Urgent
        var vmUrgent = CreateMeetingVm(
            start: DateTimeOffset.Now.AddMinutes(3),
            end: DateTimeOffset.Now.AddHours(1));
        vmUrgent.UpdateCountdown(DateTimeOffset.Now, showJoinButton: false);
        vmUrgent.IsUrgent.Should().BeTrue();

        // Live
        var vmLive = CreateMeetingVm(
            start: DateTimeOffset.Now.AddSeconds(-10),
            end: DateTimeOffset.Now.AddHours(1));
        vmLive.UpdateCountdown(DateTimeOffset.Now, showJoinButton: false);
        vmLive.IsUrgent.Should().BeTrue();
    }

    [Fact]
    public void UpdateCountdown_IsUrgent_FalseForNormalAndElapsed()
    {
        // Normal
        var vmNormal = CreateMeetingVm(
            start: DateTimeOffset.Now.AddMinutes(20),
            end: DateTimeOffset.Now.AddHours(1));
        vmNormal.UpdateCountdown(DateTimeOffset.Now, showJoinButton: false);
        vmNormal.IsUrgent.Should().BeFalse();

        // Elapsed
        var vmElapsed = CreateMeetingVm(
            start: DateTimeOffset.Now.AddMinutes(-5),
            end: DateTimeOffset.Now.AddHours(1));
        vmElapsed.UpdateCountdown(DateTimeOffset.Now, showJoinButton: false);
        vmElapsed.IsUrgent.Should().BeFalse();
    }

    #endregion

    #region Helpers

    private static MeetingSillItemViewModel CreateMeetingVm(
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        string? location = null,
        bool hasVideoCall = false)
    {
        var evt = new CalendarEvent
        {
            Id = Guid.NewGuid().ToString(),
            CalendarId = "cal-1",
            AccountId = "acc-1",
            Title = "Test Meeting",
            StartTime = start ?? DateTimeOffset.Now.AddMinutes(20),
            EndTime = end ?? DateTimeOffset.Now.AddHours(1),
            Location = location,
            VideoCall = hasVideoCall
                ? new VideoCallInfo(new Uri("https://teams.microsoft.com/l/meetup/123"), VideoCallProvider.MicrosoftTeams)
                : null,
            ProviderType = CalendarProviderType.Outlook,
        };
        return new MeetingSillItemViewModel(evt);
    }

    #endregion
}
