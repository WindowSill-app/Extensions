namespace WindowSill.Date.Settings;

/// <summary>
/// Specifies the notification style for upcoming meetings.
/// </summary>
internal enum NotificationMode
{
    /// <summary>
    /// No notification is shown.
    /// </summary>
    None,

    /// <summary>
    /// A full-screen overlay notification is shown on all monitors.
    /// </summary>
    FullScreen,

    /// <summary>
    /// A Windows toast notification is shown.
    /// </summary>
    Toast,
}
