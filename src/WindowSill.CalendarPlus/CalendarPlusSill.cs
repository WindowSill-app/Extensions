using System.Collections.ObjectModel;
using System.ComponentModel.Composition;

using Microsoft.UI.Xaml.Media.Imaging;

using WindowSill.API;
using WindowSill.CalendarPlus.Core.Abstractions;
using WindowSill.CalendarPlus.Core.Services;
using WindowSill.CalendarPlus.Settings;
using WindowSill.CalendarPlus.ViewModels;

namespace WindowSill.CalendarPlus;

/// <summary>
/// Entry point for the Calendar Plus extension.
/// Provides calendar events, world clocks, and meeting reminders in the sill bar.
/// </summary>
[Export(typeof(ISill))]
[Name("Calendar Plus")]
[Priority(Priority.High)]
[SupportMultipleMonitors(showOnEveryMonitorsByDefault: true)]
public sealed class CalendarPlusSill : ISillActivatedByDefault, ISillListView, ISillFirstTimeSetup
{
    private readonly ISettingsProvider _settingsProvider;
    private readonly IPluginInfo _pluginInfo;
    private readonly Lazy<CalendarService> _calendarService;
    private readonly Lazy<IMeetingDetector> _meetingDetector;
    private readonly Lazy<IReminderScheduler> _reminderScheduler;
    private readonly Lazy<WorldClockService> _worldClockService;
    private readonly Lazy<CalendarPlusViewModel> _viewModel;
    private CancellationTokenSource? _activationCts;

    /// <summary>
    /// MEF constructor for dependency injection.
    /// </summary>
    [ImportingConstructor]
    internal CalendarPlusSill(
        ISettingsProvider settingsProvider,
        IPluginInfo pluginInfo,
        Lazy<CalendarService> calendarService,
        Lazy<IMeetingDetector> meetingDetector,
        Lazy<IReminderScheduler> reminderScheduler,
        Lazy<WorldClockService> worldClockService)
    {
        _settingsProvider = settingsProvider;
        _pluginInfo = pluginInfo;
        _calendarService = calendarService;
        _meetingDetector = meetingDetector;
        _reminderScheduler = reminderScheduler;
        _worldClockService = worldClockService;

        _viewModel = new Lazy<CalendarPlusViewModel>(() =>
            new CalendarPlusViewModel(
                _calendarService.Value,
                _meetingDetector.Value,
                _worldClockService.Value,
                _settingsProvider));
    }

    /// <inheritdoc/>
    public string DisplayName => "/WindowSill.CalendarPlus/Misc/DisplayName".GetLocalizedString();

    /// <inheritdoc/>
    public IconElement CreateIcon()
        => new ImageIcon
        {
            Source = new SvgImageSource(new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "calendar.svg")))
        };

    /// <inheritdoc/>
    public SillSettingsView[]? SettingsViews =>
    [
        new SillSettingsView(
            DisplayName,
            new(() => new SettingsView(_settingsProvider)))
    ];

    /// <inheritdoc/>
    public ObservableCollection<SillListViewItem> ViewList => _viewModel.Value.ViewList;

    /// <inheritdoc/>
    public SillView? PlaceholderView => null;

    /// <inheritdoc/>
    public async ValueTask OnActivatedAsync()
    {
        _activationCts = new CancellationTokenSource();

        int syncMinutes = _settingsProvider.GetSetting(Settings.Settings.SyncIntervalMinutes);
        await _calendarService.Value.StartPeriodicRefreshAsync(
            TimeSpan.FromMinutes(syncMinutes),
            _activationCts.Token);

        int reminderMinutes = _settingsProvider.GetSetting(Settings.Settings.ReminderMinutesBefore);
        ((ReminderSchedulerService)_reminderScheduler.Value).SetReminderMinutes(reminderMinutes);
        _reminderScheduler.Value.ReminderTriggered += OnReminderTriggered;
        await _reminderScheduler.Value.StartAsync(_activationCts.Token);

        _viewModel.Value.StartUpdating(_activationCts.Token);
    }

    /// <inheritdoc/>
    public async ValueTask OnDeactivatedAsync()
    {
        _reminderScheduler.Value.ReminderTriggered -= OnReminderTriggered;

        _activationCts?.Cancel();
        _activationCts?.Dispose();
        _activationCts = null;

        await _reminderScheduler.Value.StopAsync();
    }

    private void OnReminderTriggered(object? sender, ReminderEventArgs e)
    {
        bool useFullScreen = _settingsProvider.GetSetting(Settings.Settings.UseFullScreenNotification);

        if (useFullScreen)
        {
            ThreadHelper.RunOnUIThreadAsync(async () =>
            {
                var window = new Views.FullScreenReminderWindow(e.CalendarEvent, _meetingDetector.Value);
                await window.ShowAsync();
            }).Forget();
        }
        else
        {
            // Toast notification
            ShowToastNotification(e.CalendarEvent, e.TimeUntilStart);
        }
    }

    private static void ShowToastNotification(Core.Models.CalendarEvent calendarEvent, TimeSpan timeUntilStart)
    {
        var toastContent = new Microsoft.Windows.AppNotifications.Builder.AppNotificationBuilder()
            .AddText(calendarEvent.Subject)
            .AddText(string.Format(
                "/WindowSill.CalendarPlus/Agenda/InMinutes".GetLocalizedString(),
                (int)Math.Ceiling(timeUntilStart.TotalMinutes)));

        if (!string.IsNullOrEmpty(calendarEvent.OnlineMeetingUrl))
        {
            toastContent.AddButton(
                new Microsoft.Windows.AppNotifications.Builder.AppNotificationButton(
                    "/WindowSill.CalendarPlus/Agenda/JoinMeeting".GetLocalizedString())
                    .AddArgument("action", "join")
                    .AddArgument("url", calendarEvent.OnlineMeetingUrl));
        }

        Microsoft.Windows.AppNotifications.AppNotificationManager.Default.Show(toastContent.BuildNotification());
    }

    /// <inheritdoc/>
    public IFirstTimeSetupContributor[] GetFirstTimeSetupContributors()
    {
        return [new FirstTimeSetup.CalendarFirstTimeSetupContributor()];
    }
}
