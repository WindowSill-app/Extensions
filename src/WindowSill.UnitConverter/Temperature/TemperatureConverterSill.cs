using Microsoft.UI.Xaml.Media.Imaging;

using System.Collections.ObjectModel;
using System.ComponentModel.Composition;

using WindowSill.API;

namespace WindowSill.UnitConverter.Temperature;

[Export(typeof(ISill))]
[Name("WindowSill.TemperatureConverterSill")]
public sealed class TemperatureConverterSill : ISillActivatedByTextSelection, ISillListView
{
    [Import]
    private IPluginInfo _pluginInfo = null!;

    private readonly TemperatureConverterViewModel _viewModel = new();

    public string DisplayName => "/WindowSill.UnitConverter/Temperature/DisplayName".GetLocalizedString();

    public IconElement CreateIcon()
        => new ImageIcon
        {
            Source = new SvgImageSource(new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "temperature.svg")))
        };

    public SillSettingsView[]? SettingsViews => throw new NotImplementedException();

    public ObservableCollection<SillListViewItem> ViewList { get; } = new();

    public SillView? PlaceholderView => throw new NotImplementedException();

    public string[] TextSelectionActivatorTypeNames { get; } = [TemperatureActivator.ActivatorName];

    public async ValueTask OnActivatedAsync(string textSelectionActivatorTypeName, WindowTextSelection currentSelection)
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            ViewList.Clear();

            if (textSelectionActivatorTypeName == TemperatureActivator.ActivatorName)
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
