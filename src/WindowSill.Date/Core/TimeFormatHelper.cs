using System.Globalization;

using WindowSill.API;
using WindowSill.Date.Settings;

namespace WindowSill.Date.Core;

/// <summary>
/// Shared helper for resolving the user's preferred time format string.
/// </summary>
internal static class TimeFormatHelper
{
    /// <summary>
    /// Gets the effective time format string based on the user's settings.
    /// Falls back to the culture's default short time pattern when the user
    /// has not selected a specific format.
    /// </summary>
    /// <param name="settingsProvider">The settings provider.</param>
    /// <param name="showSeconds">Whether to include seconds in the format.</param>
    /// <returns>A .NET time format string (e.g., "h:mm tt", "HH:mm").</returns>
    public static string GetTimeFormatString(ISettingsProvider settingsProvider, bool showSeconds = false)
    {
        TimeFormat userFormat = settingsProvider.GetSetting(Settings.Settings.TimeFormat);

        return userFormat == TimeFormat.None
            ? CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern
            : userFormat.ToFormatString(showSeconds);
    }
}
