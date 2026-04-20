using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.UI.Xaml.Media.Imaging;
using WindowSill.API;
using WindowSill.Date.Core;
using WindowSill.Date.Core.Services;
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
[SupportMultipleMonitors(showOnEveryMonitorsByDefault: true)]
[HideIconInSillListView]
internal sealed class DateSill : ISillActivatedByDefault, ISillListView, ISillFirstTimeSetup
{
    private readonly IPluginInfo _pluginInfo;
    private readonly ISettingsProvider _settingsProvider;
    private readonly CalendarAccountManager _calendarAccountManager;
    private readonly WorldClockService _worldClockService;

    private DateBarViewModel? _dateBarViewModel;
    private DatePopupViewModel? _popupViewModel;

    [ImportingConstructor]
    public DateSill(
        IPluginInfo pluginInfo,
        ISettingsProvider settingsProvider,
        CalendarAccountManager calendarAccountManager,
        WorldClockService worldClockService)
    {
        _pluginInfo = pluginInfo;
        _settingsProvider = settingsProvider;
        _calendarAccountManager = calendarAccountManager;
        _worldClockService = worldClockService;
    }

    /// <inheritdoc/>
    public string DisplayName => "Date";

    /// <inheritdoc/>
    public SillSettingsView[]? SettingsViews =>
    [
        new SillSettingsView(
            DisplayName,
            new(() => new SettingsView(_settingsProvider, _calendarAccountManager, _pluginInfo.GetPluginContentDirectory()))),
        new SillSettingsView(
            "/WindowSill.Date/WorldClocks/SettingsTabName".GetLocalizedString(),
            new(() => new WorldClockSettingsView(_worldClockService))),
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
        throw new NotImplementedException();
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
                SillListViewPopupItem barItem = DateBarContent.CreateViewListItem(_dateBarViewModel, popupView);
                ViewList.Add(barItem);
            }
        });
    }

    /// <inheritdoc/>
    public async ValueTask OnDeactivatedAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            _popupViewModel?.Dispose();
            _popupViewModel = null;
            _dateBarViewModel?.Dispose();
            _dateBarViewModel = null;
            ViewList.Clear();
        });
    }
}
