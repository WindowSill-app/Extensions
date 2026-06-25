using Microsoft.UI.Xaml.Media.Imaging;

using System.Collections.ObjectModel;
using System.ComponentModel.Composition;

using Windows.System;

using WindowSill.API;
using WindowSill.WebBrowser.View;

namespace WindowSill.WebBrowser;

[Export(typeof(ISill))]
[Name("Web Browsers")]
[Priority(Priority.Lowest)]
public sealed class WebBrowserSill : ISillActivatedByProcess, ISillListView
{
    private readonly IProcessInteractionService _processInteractionService;
    private readonly IPluginInfo _pluginInfo;

    private WindowInfo? _activeProcessWindow;

    [ImportingConstructor]
    public WebBrowserSill(IProcessInteractionService processInteractionService, IPluginInfo pluginInfo)
    {
        _processInteractionService = processInteractionService;
        _pluginInfo = pluginInfo;
    }

    public string DisplayName => "/WindowSill.WebBrowser/Misc/DisplayName".GetLocalizedString();

    public IconElement CreateIcon()
        => new ImageIcon
        {
            Source = new SvgImageSource(new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "globe.svg")))
        };

    public ObservableCollection<SillListViewItem> ViewList
        => [
            new SillListViewButtonItem(
                new ZoomInListItemContent(),
                "/WindowSill.WebBrowser/Misc/ZoomIn".GetLocalizedString(),
                OnZoomInButtonClickAsync),

            new SillListViewButtonItem(
                new ResetZoomListItemContent(),
                "/WindowSill.WebBrowser/Misc/ResetZoom".GetLocalizedString(),
                OnResetZoomButtonClickAsync),

            new SillListViewButtonItem(
                new ZoomOutListItemContent(),
                "/WindowSill.WebBrowser/Misc/ZoomOut".GetLocalizedString(),
                OnZoomOutButtonClickAsync)
        ];

    public SillView? PlaceholderView => throw new NotImplementedException();

    public SillSettingsView[]? SettingsViews => throw new NotImplementedException();

    public string[] ProcessActivatorTypeNames =>
        [
        "Microsoft Edge",
        "Google Chrome",
        "Mozilla Firefox",
        "Brave",
        "Opera",
        "Vivaldi",
        "Zen"
        ];

    public ValueTask OnActivatedAsync(string processActivatorTypeName, WindowInfo currentProcessWindow)
    {
        _activeProcessWindow = currentProcessWindow;
        return ValueTask.CompletedTask;
    }

    public ValueTask OnDeactivatedAsync()
    {
        _activeProcessWindow = null;
        return ValueTask.CompletedTask;
    }

    private async Task OnZoomInButtonClickAsync()
    {
        // The list item button can be invoked after the browser window lost focus and the sill was
        // deactivated, which clears the tracked window. Treat that as a no-op instead of throwing.
        WindowInfo? activeProcessWindow = _activeProcessWindow;
        if (activeProcessWindow is null)
        {
            return;
        }

        await _processInteractionService.SimulateKeysOnWindow(
            activeProcessWindow,
            VirtualKey.LeftControl,
            VirtualKey.Add);
    }

    private async Task OnResetZoomButtonClickAsync()
    {
        WindowInfo? activeProcessWindow = _activeProcessWindow;
        if (activeProcessWindow is null)
        {
            return;
        }

        await _processInteractionService.SimulateKeysOnWindow(
            activeProcessWindow,
            VirtualKey.LeftControl,
            VirtualKey.Number0);
    }

    private async Task OnZoomOutButtonClickAsync()
    {
        WindowInfo? activeProcessWindow = _activeProcessWindow;
        if (activeProcessWindow is null)
        {
            return;
        }

        await _processInteractionService.SimulateKeysOnWindow(
            activeProcessWindow,
            VirtualKey.LeftControl,
            VirtualKey.Subtract);
    }
}
