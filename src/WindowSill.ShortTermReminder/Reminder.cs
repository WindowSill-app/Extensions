namespace WindowSill.ShortTermReminder;

internal sealed record Reminder
{
    public string Title { get; set; } = string.Empty;

    public TimeSpan OriginalReminderDuration { get; set; } = TimeSpan.Zero;

    public DateTime ReminderTime { get; set; } = DateTime.MinValue;

    public Guid Id { get; set; } = Guid.NewGuid();
}
