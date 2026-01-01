using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.UI.Xaml.Media.Imaging;
using WindowSill.API;
using Path = System.IO.Path;

namespace WindowSill.URLHelper;

[Export(typeof(ISill))]
[Name("URL Tool")]
public sealed class URLHelperSill : ISillActivatedByTextSelection, ISillListView
{
    [Import]
    private IPluginInfo _pluginInfo = null!;

    public string DisplayName => "/WindowSill.URLHelper/Misc/DisplayName".GetLocalizedString();

    public IconElement CreateIcon()
        => new ImageIcon
        {
            Source = new SvgImageSource(new Uri(Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "link.svg")))
        };

    public ObservableCollection<SillListViewItem> ViewList { get; } = new();

    public SillView? PlaceholderView => throw new NotImplementedException();

    public SillSettingsView[]? SettingsViews => throw new NotImplementedException();

    public string[] TextSelectionActivatorTypeNames => [PredefinedActivationTypeNames.UriSelection];

    public async ValueTask OnActivatedAsync(string textSelectionActivatorTypeName, WindowTextSelection currentSelection)
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            ViewList.Clear();

            ViewList.Add(
                new SillListViewPopupItem(
                    "/WindowSill.URLHelper/ShortenURL/Title".GetLocalizedString(),
                    null,
                    ShortenURLSillPopup.CreateView(currentSelection)));

            ViewList.Add(
                new SillListViewPopupItem(
                    "/WindowSill.URLHelper/QRCode/Title".GetLocalizedString(),
                    null,
                    QRCodeSillPopup.CreateView(currentSelection)));
        });
    }

    public async ValueTask OnDeactivatedAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            ViewList.Clear();
        });
    }
}
