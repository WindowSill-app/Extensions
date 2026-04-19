using System.Globalization;

namespace WindowSill.Date.Settings;

/// <summary>
/// Available time display formats for the sill bar.
/// </summary>
internal enum TimeFormat
{
    /// <summary>
    /// Do not display the time.
    /// </summary>
    None,

    /// <summary>
    /// 12-hour format with AM/PM. Example: "11:28 AM".
    /// </summary>
    TwelveHour,

    /// <summary>
    /// 24-hour format. Example: "23:28".
    /// </summary>
    TwentyFourHour,
}

/// <summary>
/// Extension methods for <see cref="TimeFormat"/>.
/// </summary>
internal static class TimeFormatExtensions
{
    /// <summary>
    /// Formats the given <see cref="DateTime"/> using this time format and the current culture.
    /// Falls back to invariant culture for 12-hour format when the culture lacks AM/PM designators.
    /// </summary>
    /// <param name="format">The time format option.</param>
    /// <param name="dateTime">The date/time to format.</param>
    /// <param name="showSeconds">Whether to include seconds in the output.</param>
    /// <returns>The formatted time string, or empty for <see cref="TimeFormat.None"/>.</returns>
    public static string FormatTime(this TimeFormat format, DateTime dateTime, bool showSeconds)
    {
        if (format == TimeFormat.None)
        {
            return string.Empty;
        }

        string formatString = format switch
        {
            TimeFormat.TwelveHour => showSeconds ? "h:mm:ss tt" : "h:mm tt",
            TimeFormat.TwentyFourHour => showSeconds ? "HH:mm:ss" : "HH:mm",
            _ => string.Empty,
        };

        if (format == TimeFormat.TwelveHour
            && string.IsNullOrEmpty(CultureInfo.CurrentCulture.DateTimeFormat.AMDesignator))
        {
            // Culture doesn't have AM/PM designators (e.g., French); fall back to invariant.
            return dateTime.ToString(formatString, CultureInfo.InvariantCulture);
        }

        return dateTime.ToString(formatString, CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Returns the .NET format string for display preview purposes.
    /// </summary>
    /// <param name="format">The time format option.</param>
    /// <param name="showSeconds">Whether to include seconds.</param>
    /// <returns>A .NET time format string, or empty for <see cref="TimeFormat.None"/>.</returns>
    public static string ToFormatString(this TimeFormat format, bool showSeconds) => format switch
    {
        TimeFormat.None => string.Empty,
        TimeFormat.TwelveHour => showSeconds ? "h:mm:ss tt" : "h:mm tt",
        TimeFormat.TwentyFourHour => showSeconds ? "HH:mm:ss" : "HH:mm",
        _ => string.Empty,
    };
}
