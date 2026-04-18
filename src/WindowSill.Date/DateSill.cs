using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.UI.Xaml.Media.Imaging;
using WindowSill.API;
using WindowSill.Date.Core;
using WindowSill.Date.Views;

namespace WindowSill.Date;

[Export(typeof(ISill))]
[Name("Date")]
[Priority(Priority.Lowest)]
[SupportMultipleMonitors(showOnEveryMonitorsByDefault: true)]
internal sealed class DateSill : ISillActivatedByDefault, ISillListView, ISillFirstTimeSetup
{
    private readonly IPluginInfo _pluginInfo;
    private readonly ISettingsProvider _settingsProvider;
    private readonly CalendarAccountManager _calendarAccountManager;

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
    public SillView? PlaceholderView => throw new NotImplementedException();

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
    public ValueTask OnActivatedAsync()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public ValueTask OnDeactivatedAsync()
    {
        throw new NotImplementedException();
    }
}
