using System;
using CommunityToolkit.Mvvm.ComponentModel;
using WindowSill.Date.Core.Models;

namespace WindowSill.Date.ViewModels;

/// <summary>
/// View model wrapping a <see cref="CalendarInfo"/> with a user-togglable visibility state.
/// </summary>
internal sealed partial class CalendarViewModel : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarViewModel"/> class.
    /// </summary>
    /// <param name="calendarInfo">The calendar metadata.</param>
    /// <param name="isVisible">Whether this calendar is visible (not hidden).</param>
    public CalendarViewModel(CalendarInfo calendarInfo, bool isVisible)
    {
        CalendarInfo = calendarInfo;
        IsVisible = isVisible;
    }

    /// <summary>
    /// Gets the underlying calendar info.
    /// </summary>
    public CalendarInfo CalendarInfo { get; }

    /// <summary>
    /// Gets the calendar ID.
    /// </summary>
    public string Id => CalendarInfo.Id;

    /// <summary>
    /// Gets the display name of the calendar.
    /// </summary>
    public string Name => CalendarInfo.Name;

    /// <summary>
    /// Gets the color associated with this calendar.
    /// </summary>
    public string? Color => CalendarInfo.Color;

    /// <summary>
    /// Gets or sets a value indicating whether this calendar is visible (events shown).
    /// </summary>
    [ObservableProperty]
    public partial bool IsVisible { get; set; }

    /// <summary>
    /// Raised when <see cref="IsVisible"/> changes.
    /// </summary>
    public event EventHandler? VisibilityChanged;

    partial void OnIsVisibleChanged(bool value)
    {
        VisibilityChanged?.Invoke(this, EventArgs.Empty);
    }
}
