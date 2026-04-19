using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

using WindowSill.API;
using WindowSill.Date.Settings;

namespace WindowSill.Date.ViewModels;

/// <summary>
/// ViewModel for the Date sill bar content. Drives live date/time text updates
/// and reacts to display settings changes.
/// </summary>
internal sealed partial class DateBarViewModel : ObservableObject, IDisposable
{
    private readonly ISettingsProvider _settingsProvider;
    private readonly string _iconPath;

    private DispatcherQueueTimer? _timer;
    private DateFormat _dateFormat;
    private TimeFormat _timeFormat;
    private bool _showSeconds;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DateBarViewModel"/> class.
    /// </summary>
    /// <param name="settingsProvider">The settings provider for reading and observing settings.</param>
    /// <param name="contentDirectory">The plugin content directory for resolving asset paths.</param>
    public DateBarViewModel(ISettingsProvider settingsProvider, string contentDirectory)
    {
        _settingsProvider = settingsProvider;
        _iconPath = System.IO.Path.Combine(contentDirectory, "Assets", "package.svg");

        ReadSettings();
        RefreshTexts();

        _settingsProvider.SettingChanged += OnSettingChanged;
    }

    /// <summary>
    /// Gets the formatted date text for display.
    /// </summary>
    [ObservableProperty]
    public partial string DateText { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the formatted time text for display.
    /// </summary>
    [ObservableProperty]
    public partial string TimeText { get; private set; } = string.Empty;

    /// <summary>
    /// Gets whether the sill is in icon display mode.
    /// </summary>
    [ObservableProperty]
    public partial bool IsIconMode { get; private set; }

    /// <summary>
    /// Gets whether the sill is in date/time text display mode.
    /// </summary>
    [ObservableProperty]
    public partial bool IsDateTimeMode { get; private set; }

    /// <summary>
    /// Gets whether a date text is currently being displayed.
    /// </summary>
    [ObservableProperty]
    public partial bool HasDateText { get; private set; }

    /// <summary>
    /// Gets whether a time text is currently being displayed.
    /// </summary>
    [ObservableProperty]
    public partial bool HasTimeText { get; private set; }

    /// <summary>
    /// Gets the icon image source for icon display mode.
    /// </summary>
    public ImageSource IconSource => new SvgImageSource(new Uri(_iconPath));

    /// <summary>
    /// Starts the update timer on the given dispatcher queue.
    /// Aligns the first tick to the next second or minute boundary for accuracy.
    /// </summary>
    /// <param name="dispatcherQueue">The UI thread's dispatcher queue.</param>
    public void StartTimer(DispatcherQueue dispatcherQueue)
    {
        StopTimer();

        _timer = dispatcherQueue.CreateTimer();
        _timer.Tick += OnTimerTick;

        ConfigureTimerInterval();
    }

    /// <summary>
    /// Stops the update timer and releases resources.
    /// </summary>
    public void StopTimer()
    {
        if (_timer is not null)
        {
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopTimer();
        _settingsProvider.SettingChanged -= OnSettingChanged;
    }

    private void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        RefreshTexts();

        // Re-align to the next boundary after each tick to prevent drift.
        ConfigureTimerInterval();
    }

    private void OnSettingChanged(ISettingsProvider sender, SettingChangedEventArgs args)
    {
        if (args.SettingName == Settings.Settings.DisplayMode.Name
            || args.SettingName == Settings.Settings.DateFormat.Name
            || args.SettingName == Settings.Settings.TimeFormat.Name
            || args.SettingName == Settings.Settings.ShowSeconds.Name)
        {
            ReadSettings();
            RefreshTexts();

            // Timer interval may have changed (seconds toggle).
            if (_timer is not null)
            {
                ConfigureTimerInterval();
            }
        }
    }

    private void ReadSettings()
    {
        SillDisplayMode displayMode = _settingsProvider.GetSetting(Settings.Settings.DisplayMode);
        _dateFormat = _settingsProvider.GetSetting(Settings.Settings.DateFormat);
        _timeFormat = _settingsProvider.GetSetting(Settings.Settings.TimeFormat);
        _showSeconds = _settingsProvider.GetSetting(Settings.Settings.ShowSeconds);

        IsIconMode = displayMode == SillDisplayMode.Icon;
        IsDateTimeMode = displayMode == SillDisplayMode.DateTime;
    }

    private void RefreshTexts()
    {
        DateTime now = DateTime.Now;

        DateText = _dateFormat.FormatDate(now);
        TimeText = _timeFormat.FormatTime(now, _showSeconds);

        HasDateText = _dateFormat != DateFormat.None;
        HasTimeText = _timeFormat != TimeFormat.None;
    }

    /// <summary>
    /// Configures the timer interval and aligns to the next second or minute boundary.
    /// </summary>
    private void ConfigureTimerInterval()
    {
        if (_timer is null)
        {
            return;
        }

        _timer.Stop();

        if (!IsDateTimeMode)
        {
            // In icon mode, no need for frequent updates. Tick once per minute for
            // potential future use (e.g., accessibility name updates).
            _timer.Interval = TimeSpan.FromMinutes(1);
        }
        else if (_showSeconds || _timeFormat == TimeFormat.None)
        {
            // When showing seconds: tick every second.
            // When only showing date: tick once per minute is fine, but align to next minute.
            if (_showSeconds)
            {
                _timer.Interval = TimeSpan.FromSeconds(1);
            }
            else
            {
                // Align to next minute boundary.
                DateTime now = DateTime.Now;
                TimeSpan delayToNextMinute = TimeSpan.FromSeconds(60 - now.Second) - TimeSpan.FromMilliseconds(now.Millisecond);
                if (delayToNextMinute <= TimeSpan.Zero)
                {
                    delayToNextMinute = TimeSpan.FromMinutes(1);
                }

                _timer.Interval = delayToNextMinute;
            }
        }
        else
        {
            // Showing time without seconds: align to next minute boundary.
            DateTime now = DateTime.Now;
            TimeSpan delayToNextMinute = TimeSpan.FromSeconds(60 - now.Second) - TimeSpan.FromMilliseconds(now.Millisecond);
            if (delayToNextMinute <= TimeSpan.Zero)
            {
                delayToNextMinute = TimeSpan.FromMinutes(1);
            }

            _timer.Interval = delayToNextMinute;
        }

        _timer.Start();
    }
}
