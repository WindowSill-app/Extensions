using System.Globalization;

namespace WindowSill.Date.Settings;

/// <summary>
/// Extension methods for <see cref="DateFormat"/>.
/// </summary>
internal static class DateFormatExtensions
{
    /// <summary>
    /// Returns the .NET format string for the given <see cref="DateFormat"/>.
    /// </summary>
    /// <param name="format">The date format option.</param>
    /// <returns>A .NET date format string, or empty for <see cref="DateFormat.None"/>.</returns>
    public static string ToFormatString(this DateFormat format) => format switch
    {
        DateFormat.None => string.Empty,
        DateFormat.AbbreviatedDayMonth => "ddd, MMM d",
        DateFormat.ShortMonthDay => "MMM d",
        DateFormat.DayShortMonth => "d MMM",
        DateFormat.FullDayMonth => "dddd, MMMM d",
        DateFormat.MonthSlashDayCompact => "M/d",
        DateFormat.MonthSlashDay => "MM/dd",
        DateFormat.DaySlashMonthCompact => "d/M",
        DateFormat.DaySlashMonth => "dd/MM",
        DateFormat.MonthDayYear => "MM/dd/yyyy",
        DateFormat.DayMonthYear => "dd/MM/yyyy",
        DateFormat.Iso8601 => "yyyy-MM-dd",
        _ => string.Empty,
    };

    /// <summary>
    /// Formats the given <see cref="DateTime"/> using this date format and the current culture.
    /// </summary>
    /// <param name="format">The date format option.</param>
    /// <param name="dateTime">The date/time to format.</param>
    /// <returns>The formatted date string, or empty for <see cref="DateFormat.None"/>.</returns>
    public static string FormatDate(this DateFormat format, DateTime dateTime)
    {
        if (format == DateFormat.None)
        {
            return string.Empty;
        }

        return dateTime.ToString(format.ToFormatString(), CultureInfo.CurrentCulture);
    }

    /// <summary>
    /// Returns a clarifying label suffix for ambiguous numeric formats (e.g., "(M/D)").
    /// </summary>
    /// <param name="format">The date format option.</param>
    /// <returns>A label suffix string, or <see langword="null"/> if the format is self-explanatory.</returns>
    public static string? GetLabelSuffix(this DateFormat format) => format switch
    {
        DateFormat.MonthSlashDayCompact => "(M/D)",
        DateFormat.MonthSlashDay => "(MM/DD)",
        DateFormat.DaySlashMonthCompact => "(D/M)",
        DateFormat.DaySlashMonth => "(DD/MM)",
        DateFormat.MonthDayYear => "(MM/DD/YYYY)",
        DateFormat.DayMonthYear => "(DD/MM/YYYY)",
        DateFormat.Iso8601 => "(ISO 8601)",
        _ => null,
    };
}
