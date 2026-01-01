using Microsoft.UI.Xaml.Media.Imaging;

using System.Collections.ObjectModel;
using System.ComponentModel.Composition;

using WindowSill.API;

namespace WindowSill.UnitConverter.Dimension;

[Export(typeof(ISill))]
[Name("WindowSill.DimensionConverterSill")]
public sealed class DimensionConverterSill : ISillActivatedByTextSelection, ISillListView
{
    [Import]
    private IPluginInfo _pluginInfo = null!;

    private readonly DimensionConverterViewModel _viewModel = new();

    public string DisplayName => "/WindowSill.UnitConverter/Dimension/DisplayName".GetLocalizedString();

    public IconElement CreateIcon()
        => new ImageIcon
        {
            Source = new SvgImageSource(new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "dimension.svg")))
        };

    public SillSettingsView[]? SettingsViews => throw new NotImplementedException();

    public ObservableCollection<SillListViewItem> ViewList { get; } = new();

    public SillView? PlaceholderView => throw new NotImplementedException();

    public string[] TextSelectionActivatorTypeNames { get; } = [DimensionActivator.ActivatorName];

    public async ValueTask OnActivatedAsync(string textSelectionActivatorTypeName, WindowTextSelection currentSelection)
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            ViewList.Clear();

            if (textSelectionActivatorTypeName == DimensionActivator.ActivatorName)
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
