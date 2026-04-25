using FluentAssertions;

using WindowSill.Date.Core.UI;
using WindowSill.Date.Settings;

namespace UnitTests.Date.Core.UI;

/// <summary>
/// Tests for the pure layout calculator. Uses opaque object references as items
/// so the tests are free of WinUI dependencies.
/// </summary>
public class SillBarLayoutTests
{
    // Named tokens to make assertions readable.
    private readonly object _date = new();
    private readonly object _meeting1 = new();
    private readonly object _meeting2 = new();
    private readonly object _earlyClock = new();
    private readonly object _lateClock = new();

    [Fact]
    public void ComputeOrder_BeforeAll_AfterDateSill_PutsMeetingsFirst_ClocksAfterDate()
    {
        IReadOnlyList<object> ordered = SillBarLayout.ComputeOrder(
            _date,
            new[] { _meeting1, _meeting2 },
            new[] { (_earlyClock, true), (_lateClock, false) },
            MeetingPlacement.BeforeAll,
            WorldClockPlacement.AfterDateSill);

        ordered.Should().Equal(_meeting1, _meeting2, _date, _earlyClock, _lateClock);
    }

    [Fact]
    public void ComputeOrder_AfterAll_BeforeDateSill_PutsMeetingsLast_ClocksBeforeDate()
    {
        IReadOnlyList<object> ordered = SillBarLayout.ComputeOrder(
            _date,
            new[] { _meeting1, _meeting2 },
            new[] { (_earlyClock, true), (_lateClock, false) },
            MeetingPlacement.AfterAll,
            WorldClockPlacement.BeforeDateSill);

        ordered.Should().Equal(_earlyClock, _lateClock, _date, _meeting1, _meeting2);
    }

    [Fact]
    public void ComputeOrder_ByTimezone_SplitsClocksAroundDate()
    {
        IReadOnlyList<object> ordered = SillBarLayout.ComputeOrder(
            _date,
            Array.Empty<object>(),
            new[] { (_earlyClock, true), (_lateClock, false) },
            MeetingPlacement.BeforeAll,
            WorldClockPlacement.ByTimezone);

        ordered.Should().Equal(_earlyClock, _date, _lateClock);
    }

    [Fact]
    public void ComputeOrder_AfterAll_ByTimezone_KeepsMeetingsLastEvenWithSplitClocks()
    {
        IReadOnlyList<object> ordered = SillBarLayout.ComputeOrder(
            _date,
            new[] { _meeting1 },
            new[] { (_earlyClock, true), (_lateClock, false) },
            MeetingPlacement.AfterAll,
            WorldClockPlacement.ByTimezone);

        ordered.Should().Equal(_earlyClock, _date, _lateClock, _meeting1);
    }

    [Fact]
    public void ComputeOrder_NoMeetings_NoClocks_ReturnsOnlyDate()
    {
        IReadOnlyList<object> ordered = SillBarLayout.ComputeOrder(
            _date,
            Array.Empty<object>(),
            Array.Empty<(object, bool)>(),
            MeetingPlacement.BeforeAll,
            WorldClockPlacement.AfterDateSill);

        ordered.Should().Equal(_date);
    }

    [Fact]
    public void ComputeOrder_AllClocksMarkedEarlier_UnderByTimezone_AllGoBeforeDate()
    {
        IReadOnlyList<object> ordered = SillBarLayout.ComputeOrder(
            _date,
            Array.Empty<object>(),
            new[] { (_earlyClock, true), (_lateClock, true) },
            MeetingPlacement.BeforeAll,
            WorldClockPlacement.ByTimezone);

        ordered.Should().Equal(_earlyClock, _lateClock, _date);
    }

    [Fact]
    public void IndexForNewMeeting_BeforeAll_LandsAfterExistingMeetings()
    {
        var newMeeting = new object();

        int index = SillBarLayout.IndexForNewMeeting(
            _date,
            new[] { _meeting1 },
            new[] { (_earlyClock, true), (_lateClock, false) },
            newMeeting,
            MeetingPlacement.BeforeAll,
            WorldClockPlacement.ByTimezone);

        // Order: [m1, newMeeting, earlyClock, date, lateClock] → 1.
        index.Should().Be(1);
    }

    [Fact]
    public void IndexForNewMeeting_AfterAll_LandsAtEnd()
    {
        var newMeeting = new object();

        int index = SillBarLayout.IndexForNewMeeting(
            _date,
            new[] { _meeting1 },
            new[] { (_earlyClock, true), (_lateClock, false) },
            newMeeting,
            MeetingPlacement.AfterAll,
            WorldClockPlacement.ByTimezone);

        // Order: [earlyClock, date, lateClock, m1, newMeeting] → 4.
        index.Should().Be(4);
    }

    [Fact]
    public void IndexForNewMeeting_FirstMeeting_BeforeAll_GoesToZero()
    {
        var newMeeting = new object();

        int index = SillBarLayout.IndexForNewMeeting(
            _date,
            Array.Empty<object>(),
            Array.Empty<(object, bool)>(),
            newMeeting,
            MeetingPlacement.BeforeAll,
            WorldClockPlacement.AfterDateSill);

        index.Should().Be(0);
    }

    [Fact]
    public void IndexForNewMeeting_FirstMeeting_AfterAll_GoesAfterDate()
    {
        var newMeeting = new object();

        int index = SillBarLayout.IndexForNewMeeting(
            _date,
            Array.Empty<object>(),
            new[] { (_lateClock, false) },
            newMeeting,
            MeetingPlacement.AfterAll,
            WorldClockPlacement.AfterDateSill);

        // Order: [date, lateClock, newMeeting] → 2.
        index.Should().Be(2);
    }

    [Fact]
    public void IndexForNewClock_BeforeDateSill_GoesBeforeDate()
    {
        var newClock = new object();

        int index = SillBarLayout.IndexForNewClock(
            _date,
            existingMeetings: Array.Empty<object>(),
            existingClocks: Array.Empty<(object, bool)>(),
            newClock,
            newClockIsEarlierTimezone: false,
            MeetingPlacement.BeforeAll,
            WorldClockPlacement.BeforeDateSill);

        // Order: [newClock, date] → 0.
        index.Should().Be(0);
    }

    [Fact]
    public void IndexForNewClock_AfterDateSill_GoesAfterDate()
    {
        var newClock = new object();

        int index = SillBarLayout.IndexForNewClock(
            _date,
            existingMeetings: new[] { _meeting1 },
            existingClocks: Array.Empty<(object, bool)>(),
            newClock,
            newClockIsEarlierTimezone: true, // ignored under AfterDateSill
            MeetingPlacement.BeforeAll,
            WorldClockPlacement.AfterDateSill);

        // Order: [m1, date, newClock] → 2.
        index.Should().Be(2);
    }

    [Fact]
    public void IndexForNewClock_ByTimezone_EarlierGoesBeforeDate()
    {
        var newClock = new object();

        int index = SillBarLayout.IndexForNewClock(
            _date,
            existingMeetings: Array.Empty<object>(),
            existingClocks: new[] { (_lateClock, false) },
            newClock,
            newClockIsEarlierTimezone: true,
            MeetingPlacement.BeforeAll,
            WorldClockPlacement.ByTimezone);

        // Order: [newClock, date, lateClock] → 0.
        index.Should().Be(0);
    }

    [Fact]
    public void IndexForNewClock_ByTimezone_LaterGoesAfterDate()
    {
        var newClock = new object();

        int index = SillBarLayout.IndexForNewClock(
            _date,
            existingMeetings: Array.Empty<object>(),
            existingClocks: new[] { (_earlyClock, true) },
            newClock,
            newClockIsEarlierTimezone: false,
            MeetingPlacement.BeforeAll,
            WorldClockPlacement.ByTimezone);

        // Order: [earlyClock, date, newClock] → 2.
        index.Should().Be(2);
    }

    [Fact]
    public void IndexForNewClock_AfterAllMeetings_DoesNotPlaceMeetingsBetweenClockAndDate()
    {
        var newClock = new object();

        int index = SillBarLayout.IndexForNewClock(
            _date,
            existingMeetings: new[] { _meeting1, _meeting2 },
            existingClocks: Array.Empty<(object, bool)>(),
            newClock,
            newClockIsEarlierTimezone: false,
            MeetingPlacement.AfterAll,
            WorldClockPlacement.AfterDateSill);

        // Order: [date, newClock, m1, m2] → 1.
        index.Should().Be(1);
    }
}
