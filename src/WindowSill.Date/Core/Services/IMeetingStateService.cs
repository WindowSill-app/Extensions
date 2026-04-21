using Microsoft.UI.Dispatching;

using WindowSill.Date.Core.Models;
using WindowSill.Date.ViewModels;

namespace WindowSill.Date.Core.Services;

/// <summary>
/// Single source of truth for active meeting state.
/// MEF singleton shared across all DateSill instances.
/// Owns the timer, event fetch, canonical VM list, hidden set, and notification dedup.
/// </summary>
internal interface IMeetingStateService
{
    /// <summary>
    /// Raised when the canonical meeting list changes (add, remove, hide, phase transition, or ended).
    /// Always raised on the UI thread.
    /// </summary>
    event Action? MeetingsChanged;

    /// <summary>
    /// Returns a snapshot of the current active meeting VMs.
    /// VMs are owned by this service — callers must NOT dispose them.
    /// </summary>
    IReadOnlyList<MeetingSillItemViewModel> GetActiveMeetings();

    /// <summary>
    /// Hides a meeting across ALL DateSill instances.
    /// </summary>
    /// <param name="key">The meeting key to hide.</param>
    void HideMeeting(MeetingKey key);

    /// <summary>
    /// Ensures the shared timer is running. Idempotent — only the first caller starts it.
    /// </summary>
    /// <param name="dispatcherQueue">A UI-thread dispatcher queue.</param>
    void Start(DispatcherQueue dispatcherQueue);

    /// <summary>
    /// Triggers an immediate event refresh on the next tick.
    /// </summary>
    void RequestRefresh();
}
