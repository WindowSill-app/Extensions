using Microsoft.UI.Xaml.Media.Imaging;

using System.Collections.ObjectModel;
using System.ComponentModel.Composition;

using WindowSill.API;

namespace WindowSill.UnitConverter.Exchange;

[Export(typeof(ISill))]
[Name("WindowSill.ExchangeConverterSill")]
public sealed class ExchangeConverterSill : ISillActivatedByTextSelection, ISillListView
{
    [Import]
    private IPluginInfo _pluginInfo = null!;

    [Import]
    private ISettingsProvider _settingsProvider = null!;

    [Import]
    private ExchangeConverterViewModel _viewModel = null!;

    public string DisplayName => "/WindowSill.UnitConverter/Exchange/DisplayName".GetLocalizedString();

    public IconElement CreateIcon()
        => new ImageIcon
        {
            Source = new SvgImageSource(new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "exchange.svg")))
        };

    public SillSettingsView[]? SettingsViews =>
        [
        new SillSettingsView(
            "/WindowSill.UnitConverter/Exchange/DisplayName".GetLocalizedString(),
            new(() => new ExchangeSettingsView(_settingsProvider)))
        ];

    public ObservableCollection<SillListViewItem> ViewList { get; } = new();

    public SillView? PlaceholderView => throw new NotImplementedException();

    public string[] TextSelectionActivatorTypeNames { get; } = [ExchangeActivator.ActivatorName];

    public async ValueTask OnActivatedAsync(string textSelectionActivatorTypeName, WindowTextSelection currentSelection)
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            ViewList.Clear();

            if (textSelectionActivatorTypeName == ExchangeActivator.ActivatorName)
            {
                SillListViewItem viewItem = _viewModel.GetView(currentSelection);
                ViewList.Add(viewItem);
            }
        });
    }

    public async ValueTask OnDeactivatedAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(ViewList.Clear);
    }
}
