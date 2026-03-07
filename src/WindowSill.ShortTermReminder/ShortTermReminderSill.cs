using Microsoft.UI.Xaml.Media.Imaging;

using System.Collections.ObjectModel;
using System.ComponentModel.Composition;

using WindowSill.API;
using WindowSill.ShortTermReminder.Core;
using WindowSill.ShortTermReminder.Settings;

namespace WindowSill.ShortTermReminder;

[Export(typeof(ISill))]
[Name("Short Term Reminders")]
[Priority(Priority.High)]
public sealed class ShortTermReminderSill : ISillActivatedByDefault, ISillListView
{
    private readonly ISettingsProvider _settingsProvider;
    private readonly IPluginInfo _pluginInfo;
    private readonly Lazy<IReminderService> _reminderService;

    [ImportingConstructor]
    internal ShortTermReminderSill(
        ISettingsProvider settingsProvider,
        IPluginInfo pluginInfo,
        Lazy<IReminderService> reminderService)
    {
        _settingsProvider = settingsProvider;
        _pluginInfo = pluginInfo;
        _reminderService = reminderService;
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

    public ObservableCollection<SillListViewItem> ViewList => _reminderService.Value.ViewList;

    public SillView? PlaceholderView => null;

    public async ValueTask OnActivatedAsync()
    {
        await _reminderService.Value.InitializeAsync(_settingsProvider);
    }

    public ValueTask OnDeactivatedAsync()
    {
        throw new NotImplementedException();
    }
}
