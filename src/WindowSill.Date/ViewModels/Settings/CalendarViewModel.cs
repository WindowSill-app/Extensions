using CommunityToolkit.Mvvm.ComponentModel;
using WindowSill.Date.Core.Models;

namespace WindowSill.Date.ViewModels;

/// <summary>
/// View model wrapping a <see cref="CalendarInfo"/> with a user-togglable visibility state
/// and an optional color override.
/// </summary>
internal sealed partial class CalendarViewModel : ObservableObject
{
    private string? _colorOverride;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarViewModel"/> class.
    /// </summary>
    /// <param name="calendarInfo">The calendar metadata.</param>
    /// <param name="isVisible">Whether this calendar is visible (not hidden).</param>
    /// <param name="colorOverride">An optional user-defined color override (hex string).</param>
    public CalendarViewModel(CalendarInfo calendarInfo, bool isVisible, string? colorOverride = null)
    {
        CalendarInfo = calendarInfo;
        IsVisible = isVisible;
        _colorOverride = colorOverride;
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
    /// Gets the effective color for this calendar, using the user override if set,
    /// otherwise falling back to the provider-assigned color.
    /// </summary>
    public string? Color => _colorOverride ?? CalendarInfo.Color;

    /// <summary>
    /// Gets or sets a value indicating whether this calendar is visible (events shown).
    /// </summary>
    [ObservableProperty]
    public partial bool IsVisible { get; set; }

    /// <summary>
    /// Raised when <see cref="IsVisible"/> changes.
    /// </summary>
    public event EventHandler? VisibilityChanged;

    /// <summary>
    /// Raised when the user changes the calendar color.
    /// </summary>
    public event EventHandler? ColorChanged;

    /// <summary>
    /// Previews a color change without persisting. Updates the UI binding only.
    /// Call <see cref="CommitColor"/> after the user finishes choosing.
    /// </summary>
    /// <param name="hexColor">The hex color to preview.</param>
    public void PreviewColor(string? hexColor)
    {
        _colorOverride = hexColor;
        OnPropertyChanged(nameof(Color));
    }

    /// <summary>
    /// Commits the current color override, raising <see cref="ColorChanged"/>
    /// so that the change is persisted.
    /// </summary>
    public void CommitColor()
    {
        ColorChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sets a user-defined color override for this calendar and commits it.
    /// Pass <see langword="null"/> to revert to the provider-assigned color.
    /// </summary>
    /// <param name="hexColor">The hex color (e.g., "#FF5733"), or <see langword="null"/> to reset.</param>
    public void SetColor(string? hexColor)
    {
        if (_colorOverride == hexColor)
        {
            return;
        }

        PreviewColor(hexColor);
        CommitColor();
    }

    partial void OnIsVisibleChanged(bool value)
    {
        VisibilityChanged?.Invoke(this, EventArgs.Empty);
    }
}
