using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.UI.Xaml.Media.Imaging;
using WindowSill.API;
using WindowSill.Date.Core;
using WindowSill.Date.ViewModels;
using WindowSill.Date.Views;

namespace WindowSill.Date;

/// <summary>
/// Entry point for the Date extension. Shows a calendar icon or live date/time in the sill bar,
/// and will host the calendar popup and upcoming meeting items.
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

    private DateBarViewModel? _dateBarViewModel;

    [ImportingConstructor]
    public DateSill(IPluginInfo pluginInfo, ISettingsProvider settingsProvider, CalendarAccountManager calendarAccountManager)
    {
        _pluginInfo = pluginInfo;
        _settingsProvider = settingsProvider;
        _calendarAccountManager = calendarAccountManager;
    }

    /// <inheritdoc/>
    public string DisplayName => "Date";

    /// <inheritdoc/>
    public SillSettingsView[]? SettingsViews =>
    [
        new SillSettingsView(
            DisplayName,
            new(() => new SettingsView(_settingsProvider, _calendarAccountManager, _pluginInfo.GetPluginContentDirectory())))
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
                SillListViewPopupItem barItem = DateBarContent.CreateViewListItem(_dateBarViewModel);
                ViewList.Add(barItem);
            }
        });
    }

    /// <inheritdoc/>
    public async ValueTask OnDeactivatedAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            _dateBarViewModel?.Dispose();
            _dateBarViewModel = null;
            ViewList.Clear();
        });
    }
}
