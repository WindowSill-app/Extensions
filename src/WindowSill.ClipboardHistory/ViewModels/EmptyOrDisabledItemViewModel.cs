using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using WindowSill.API;
namespace WindowSill.ClipboardHistory.ViewModels;

/// <summary>
/// ViewModel for the placeholder view shown when clipboard history is empty or disabled.
/// </summary>
internal sealed partial class EmptyOrDisabledItemViewModel : ObservableObject
{
    private readonly IPluginInfo _pluginInfo;

    internal EmptyOrDisabledItemViewModel(IPluginInfo pluginInfo)
    {
        _pluginInfo = pluginInfo;
        IsClipboardHistoryEnabled = Clipboard.IsHistoryEnabled();
        Clipboard.HistoryEnabledChanged += Clipboard_HistoryEnabledChanged;
    }

    public SvgImageSource PluginIconUri => new SvgImageSource(new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "clipboard.svg")));

    /// <summary>
    /// Gets or sets whether Windows clipboard history is currently enabled.
    /// </summary>
    [ObservableProperty]
    public partial bool IsClipboardHistoryEnabled { get; set; }

    [RelayCommand]
    private async Task OpenWindowsClipboardHistoryAsync()
    {
        await Launcher.LaunchUriAsync(new Uri("ms-settings:clipboard"));
    }

    private void Clipboard_HistoryEnabledChanged(object? sender, object e)
    {
        IsClipboardHistoryEnabled = Clipboard.IsHistoryEnabled();
    }
}
