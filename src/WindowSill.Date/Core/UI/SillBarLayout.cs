using WindowSill.Date.Settings;

namespace WindowSill.Date.Core.UI;

/// <summary>
/// Pure, stateless calculator that decides where bar items appear relative to each other
/// based on <see cref="MeetingPlacement"/> and <see cref="WorldClockPlacement"/> settings.
/// Generic over the item type so the calculator stays free of WinUI dependencies and is
/// trivially unit-testable.
/// </summary>
internal static class SillBarLayout
{
    /// <summary>
    /// Computes the desired ordered list of bar items.
    /// </summary>
    /// <typeparam name="T">The opaque item type (typically <c>SillListViewItem</c>).</typeparam>
    /// <param name="dateBarItem">The central date bar item.</param>
    /// <param name="meetingItems">All meeting items currently owned.</param>
    /// <param name="clockEntries">
    /// All world-clock entries with a precomputed <c>IsEarlierTimezone</c> flag (used only
    /// when <paramref name="worldClockPlacement"/> is <see cref="WorldClockPlacement.ByTimezone"/>).
    /// The flag is computed by the caller so this calculator stays free of NodaTime / system tz.
    /// </param>
    /// <param name="meetingPlacement">The meeting placement setting.</param>
    /// <param name="worldClockPlacement">The world-clock placement setting.</param>
    /// <returns>Items in their intended bar order.</returns>
    public static IReadOnlyList<T> ComputeOrder<T>(
        T dateBarItem,
        IReadOnlyCollection<T> meetingItems,
        IReadOnlyCollection<(T Item, bool IsEarlierTimezone)> clockEntries,
        MeetingPlacement meetingPlacement,
        WorldClockPlacement worldClockPlacement)
        where T : class
    {
        var clocksBefore = new List<T>();
        var clocksAfter = new List<T>();

        foreach ((T item, bool isEarlier) in clockEntries)
        {
            bool placeBefore = worldClockPlacement switch
            {
                WorldClockPlacement.BeforeDateSill => true,
                WorldClockPlacement.AfterDateSill => false,
                WorldClockPlacement.ByTimezone => isEarlier,
                _ => false,
            };

            (placeBefore ? clocksBefore : clocksAfter).Add(item);
        }

        var ordered = new List<T>(meetingItems.Count + clockEntries.Count + 1);

        if (meetingPlacement == MeetingPlacement.BeforeAll)
        {
            ordered.AddRange(meetingItems);
        }

        ordered.AddRange(clocksBefore);
        ordered.Add(dateBarItem);
        ordered.AddRange(clocksAfter);

        if (meetingPlacement == MeetingPlacement.AfterAll)
        {
            ordered.AddRange(meetingItems);
        }

        return ordered;
    }

    /// <summary>
    /// Computes the index at which a brand-new meeting item should be inserted into a
    /// <c>ViewList</c> that already holds <paramref name="existingMeetings"/>,
    /// <paramref name="clockEntries"/>, and <paramref name="dateBarItem"/> in the correct order.
    /// </summary>
    public static int IndexForNewMeeting<T>(
        T dateBarItem,
        IReadOnlyCollection<T> existingMeetings,
        IReadOnlyCollection<(T Item, bool IsEarlierTimezone)> clockEntries,
        T newMeetingItem,
        MeetingPlacement meetingPlacement,
        WorldClockPlacement worldClockPlacement)
        where T : class
    {
        var meetings = new List<T>(existingMeetings.Count + 1);
        meetings.AddRange(existingMeetings);
        meetings.Add(newMeetingItem);

        IReadOnlyList<T> ordered = ComputeOrder(
            dateBarItem,
            meetings,
            clockEntries,
            meetingPlacement,
            worldClockPlacement);

        return IndexOfReference(ordered, newMeetingItem);
    }

    /// <summary>
    /// Computes the index at which a brand-new world-clock item should be inserted into a
    /// <c>ViewList</c> that already holds the provided existing items in the correct order.
    /// </summary>
    public static int IndexForNewClock<T>(
        T dateBarItem,
        IReadOnlyCollection<T> existingMeetings,
        IReadOnlyCollection<(T Item, bool IsEarlierTimezone)> existingClocks,
        T newClockItem,
        bool newClockIsEarlierTimezone,
        MeetingPlacement meetingPlacement,
        WorldClockPlacement worldClockPlacement)
        where T : class
    {
        var clocks = new List<(T, bool)>(existingClocks.Count + 1);
        clocks.AddRange(existingClocks);
        clocks.Add((newClockItem, newClockIsEarlierTimezone));

        IReadOnlyList<T> ordered = ComputeOrder(
            dateBarItem,
            existingMeetings,
            clocks,
            meetingPlacement,
            worldClockPlacement);

        return IndexOfReference(ordered, newClockItem);
    }

    private static int IndexOfReference<T>(IReadOnlyList<T> items, T target) where T : class
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (ReferenceEquals(items[i], target))
            {
                return i;
            }
        }

        return -1;
    }
}
