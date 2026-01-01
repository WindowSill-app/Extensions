using Microsoft.UI.Xaml.Media.Imaging;

using System.Collections.ObjectModel;
using System.ComponentModel.Composition;

using WindowSill.API;
using WindowSill.ShortTermReminder.Settings;

namespace WindowSill.ShortTermReminder;

[Export(typeof(ISill))]
[Name("Short Term Reminders")]
[Priority(Priority.High)]
public sealed class ShortTermReminderSill : ISillActivatedByDefault, ISillListView
{
    private readonly ISettingsProvider _settingsProvider;
    private readonly IPluginInfo _pluginInfo;

    [ImportingConstructor]
    internal ShortTermReminderSill(ISettingsProvider settingsProvider, IPluginInfo pluginInfo)
    {
        _settingsProvider = settingsProvider;
        _pluginInfo = pluginInfo;
    }

    public string DisplayName => "/WindowSill.ShortTermReminder/Misc/DisplayName".GetLocalizedString();

    public IconElement CreateIcon()
        => new ImageIcon
        {
            Source = new SvgImageSource(new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "alarm.svg")))
        };

    public SillSettingsView[]? SettingsViews =>
        [
        new SillSettingsView(
            DisplayName,
            new(() => new SettingsView(_settingsProvider)))
        ];

    public ObservableCollection<SillListViewItem> ViewList => ShortTermReminderService.Instance.ViewList;

    public SillView? PlaceholderView => null;

    public async ValueTask OnActivatedAsync()
    {
        await ShortTermReminderService.Instance.InitializeAsync(_settingsProvider);
    }

    public ValueTask OnDeactivatedAsync()
    {
        throw new NotImplementedException();
    }
}
