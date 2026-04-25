using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.UI.Xaml.Media.Imaging;
using NodaTime;
using WindowSill.API;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Services;
using WindowSill.Date.Core.UI;
using WindowSill.Date.FirstTimeSetup;
using WindowSill.Date.ViewModels;
using WindowSill.Date.Views;

namespace WindowSill.Date;

/// <summary>
/// Entry point for the Date extension. Shows a calendar icon or live date/time in the sill bar,
/// and hosts the calendar popup and upcoming meeting items.
/// </summary>
[Export(typeof(ISill))]
[Name("Date")]
[Priority(Priority.Lowest)]
[SupportMultipleMonitors(showOnEveryMonitorsByDefault: false)]
[HideIconInSillListView]
internal sealed class DateSill : ISillActivatedByDefault, ISillListView, ISillFirstTimeSetup
{
    private readonly IPluginInfo _pluginInfo;
    private readonly ISettingsProvider _settingsProvider;
    private readonly CalendarAccountManager _calendarAccountManager;
    private readonly WorldClockService _worldClockService;
    private readonly MeetingStateService _meetingStateService;
    private readonly IGeoLocationService _geoLocationService;

    private DateBarViewModel? _dateBarViewModel;
    private DatePopupViewModel? _popupViewModel;
    private MeetingViewListAdapter? _viewListAdapter;
    private WorldClockViewListAdapter? _worldClockViewListAdapter;
    private SillListViewPopupItem? _dateBarItem;

    [ImportingConstructor]
    public DateSill(
        IPluginInfo pluginInfo,
        ISettingsProvider settingsProvider,
        CalendarAccountManager calendarAccountManager,
        WorldClockService worldClockService,
        MeetingStateService meetingStateService,
        IGeoLocationService geoLocationService)
    {
        _pluginInfo = pluginInfo;
        _settingsProvider = settingsProvider;
        _calendarAccountManager = calendarAccountManager;
        _worldClockService = worldClockService;
        _meetingStateService = meetingStateService;
        _geoLocationService = geoLocationService;
    }

    /// <inheritdoc/>
    public string DisplayName => "Date";

    /// <inheritdoc/>
    public SillSettingsView[]? SettingsViews =>
    [
        new SillSettingsView(
            "/WindowSill.Date/Settings/AccountsTabName".GetLocalizedString(),
            new(() => new AccountsSettingsView(_settingsProvider, _calendarAccountManager, _pluginInfo.GetPluginContentDirectory(), _meetingStateService))),
        new SillSettingsView(
            "/WindowSill.Date/Display/DateTimeTabName".GetLocalizedString(),
            new(() => new DateTimeSettingsView(_settingsProvider))),
        new SillSettingsView(
            "/WindowSill.Date/WorldClocks/SettingsTabName".GetLocalizedString(),
            new(() => new WorldClockSettingsView(_worldClockService, _settingsProvider))),
        new SillSettingsView(
            "/WindowSill.Date/Meetings/SettingsTabName".GetLocalizedString(),
            new(() => new MeetingSettingsView(_settingsProvider))),
    ];

    /// <inheritdoc/>
    public ObservableCollection<SillListViewItem> ViewList { get; } = [];

    /// <inheritdoc/>
    public SillView? PlaceholderView => null;

    /// <inheritdoc/>
    public IconElement CreateIcon()
        => new ImageIcon
        {
            Source = new SvgImageSource(new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "package.svg")))
        };

    /// <inheritdoc/>
    public IFirstTimeSetupContributor[] GetFirstTimeSetupContributors()
    {
        var setupState = new DateFirstTimeSetupState();

        return
        [
            new WelcomeSetupContributor(),
            new AccountsSetupContributor(_calendarAccountManager, _pluginInfo.GetPluginContentDirectory(), setupState),
            new TravelTimeSetupContributor(_settingsProvider, _geoLocationService, setupState, _meetingStateService),
        ];
    }

    /// <inheritdoc/>
    public async ValueTask OnActivatedAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            if (_dateBarViewModel is null)
            {
                _dateBarViewModel = new DateBarViewModel(_settingsProvider, _pluginInfo.GetPluginContentDirectory());
                _popupViewModel = new DatePopupViewModel(_calendarAccountManager, _worldClockService, _settingsProvider);

                var popupView = new DatePopupView(_popupViewModel);
                _dateBarItem = DateBarContent.CreateViewListItem(_dateBarViewModel, popupView);
                ViewList.Add(_dateBarItem);

                // Start the singleton meeting state service (idempotent — first instance wins).
                _meetingStateService.Start(popupView.DispatcherQueue);

                // Create per-instance adapter that syncs this ViewList with shared state.
                _viewListAdapter = new MeetingViewListAdapter(
                    _meetingStateService,
                    _worldClockService,
                    _settingsProvider,
                    ViewList)
                { ResolveInsertIndex = ResolveMeetingInsertIndex };
                _viewListAdapter.Start();

                // Create per-instance adapter for pinned world clocks.
                _worldClockViewListAdapter = new WorldClockViewListAdapter(
                    _worldClockService,
                    _settingsProvider,
                    ViewList)
                { ResolveInsertIndex = ResolveWorldClockInsertIndex };
                _worldClockViewListAdapter.Start(popupView.DispatcherQueue);

                // Refresh meetings when popup closes.
                _popupViewModel.PopupClosed += OnPopupClosed;

                // Reorder when placement settings change.
                _settingsProvider.SettingChanged += OnSettingChanged;
            }
        });
    }

    /// <inheritdoc/>
    public async ValueTask OnDeactivatedAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            _settingsProvider.SettingChanged -= OnSettingChanged;

            _worldClockViewListAdapter?.Dispose();
            _worldClockViewListAdapter = null;

            _viewListAdapter?.Dispose();
            _viewListAdapter = null;

            if (_popupViewModel is not null)
            {
                _popupViewModel.PopupClosed -= OnPopupClosed;
            }

            _popupViewModel?.Dispose();
            _popupViewModel = null;
            _dateBarViewModel?.Dispose();
            _dateBarViewModel = null;
            ViewList.Clear();
        });
    }

    private void OnPopupClosed() => _meetingStateService.RequestRefresh();

    private void OnSettingChanged(ISettingsProvider sender, SettingChangedEventArgs args)
    {
        if (args.SettingName == Settings.Settings.WorldClockPlacement.Name
            || args.SettingName == Settings.Settings.MeetingPlacement.Name)
        {
            ReorderViewList();
        }
    }

    /// <summary>
    /// Reorders the existing ViewList items based on the world clock and meeting placement
    /// settings. Used only when those settings change at runtime — items are already
    /// <c>Loaded</c> at that point, so <see cref="ObservableCollection{T}.Move"/> is safe.
    /// New items use <see cref="ResolveMeetingInsertIndex"/> or
    /// <see cref="ResolveWorldClockInsertIndex"/> to be inserted at the correct index
    /// directly, avoiding a separate reorder pass.
    /// </summary>
    private void ReorderViewList()
    {
        if (_dateBarItem is null || _viewListAdapter is null || _worldClockViewListAdapter is null)
        {
            return;
        }

        IReadOnlyList<SillListViewItem> ordered = SillBarLayout.ComputeOrder(
            _dateBarItem,
            _viewListAdapter.GetSillItems(),
            BuildClockEntries(_worldClockViewListAdapter),
            _settingsProvider.GetSetting(Settings.Settings.MeetingPlacement),
            _settingsProvider.GetSetting(Settings.Settings.WorldClockPlacement));

        for (int i = 0; i < ordered.Count; i++)
        {
            int currentIndex = ViewList.IndexOf(ordered[i]);
            if (currentIndex >= 0 && currentIndex != i)
            {
                ViewList.Move(currentIndex, i);
            }
        }
    }

    /// <summary>
    /// Resolves the index at which a brand-new meeting sill item should be inserted into
    /// <see cref="ViewList"/>. Called by <see cref="MeetingViewListAdapter"/> before
    /// <c>Insert</c>, so the item lands directly in its final position and is never moved.
    /// </summary>
    private int ResolveMeetingInsertIndex(SillListViewItem newMeetingItem)
    {
        if (_dateBarItem is null || _viewListAdapter is null || _worldClockViewListAdapter is null)
        {
            return ViewList.Count;
        }

        return SillBarLayout.IndexForNewMeeting(
            _dateBarItem,
            _viewListAdapter.GetSillItems(),
            BuildClockEntries(_worldClockViewListAdapter),
            newMeetingItem,
            _settingsProvider.GetSetting(Settings.Settings.MeetingPlacement),
            _settingsProvider.GetSetting(Settings.Settings.WorldClockPlacement));
    }

    /// <summary>
    /// Resolves the index at which a brand-new world-clock sill item should be inserted
    /// into <see cref="ViewList"/>.
    /// </summary>
    private int ResolveWorldClockInsertIndex(SillListViewItem newClockItem, WorldClockSillItemViewModel newClockVm)
    {
        if (_dateBarItem is null || _viewListAdapter is null || _worldClockViewListAdapter is null)
        {
            return ViewList.Count;
        }

        return SillBarLayout.IndexForNewClock(
            _dateBarItem,
            _viewListAdapter.GetSillItems(),
            BuildClockEntries(_worldClockViewListAdapter),
            newClockItem,
            IsEarlierTimezone(newClockVm),
            _settingsProvider.GetSetting(Settings.Settings.MeetingPlacement),
            _settingsProvider.GetSetting(Settings.Settings.WorldClockPlacement));
    }

    private static IReadOnlyCollection<(SillListViewItem Item, bool IsEarlierTimezone)> BuildClockEntries(
        WorldClockViewListAdapter adapter)
    {
        return adapter.GetEntries()
            .Select(e => ((SillListViewItem)e.SillItem, IsEarlierTimezone(e.Vm)))
            .ToList();
    }

    /// <summary>
    /// Returns <see langword="true"/> if the world clock's timezone is earlier
    /// (behind) the local timezone — i.e., it has a smaller UTC offset.
    /// </summary>
    private static bool IsEarlierTimezone(WorldClockSillItemViewModel vm)
    {
        Instant now = SystemClock.Instance.GetCurrentInstant();
        DateTimeZone localZone = DateTimeZoneProviders.Tzdb.GetSystemDefault();
        DateTimeZone remoteZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(vm.TimeZoneId) ?? DateTimeZone.Utc;

        long localOffsetMs = localZone.GetUtcOffset(now).Milliseconds;
        long remoteOffsetMs = remoteZone.GetUtcOffset(now).Milliseconds;

        return remoteOffsetMs < localOffsetMs;
    }
}
