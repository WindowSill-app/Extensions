namespace WindowSill.Date.Core.Models;

/// <summary>
/// Identifies the calendar service provider.
/// </summary>
public enum CalendarProviderType
{
    /// <summary>Microsoft Outlook via Microsoft Graph API.</summary>
    Outlook,

    /// <summary>Google Calendar via Google Calendar API.</summary>
    Google,

    /// <summary>Apple iCloud Calendar via CalDAV with Apple-specific endpoints.</summary>
    ICloud,

    /// <summary>Generic CalDAV server (RFC 4791).</summary>
    CalDav,
}
