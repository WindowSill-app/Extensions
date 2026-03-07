using Microsoft.UI.Xaml.Media.Imaging;

using System.ComponentModel.Composition;

using WindowSill.API;
using WindowSill.PerfCounter.Services;
using WindowSill.PerfCounter.Settings;
using WindowSill.PerfCounter.ViewModels;
using WindowSill.PerfCounter.Views;

namespace WindowSill.PerfCounter;

/// <summary>
/// Entry point for the Performance Counter extension.
/// </summary>
[Export(typeof(ISill))]
[Name("Performance Counter")]
[Priority(Priority.Lowest)]
[SupportMultipleMonitors]
public sealed class PerformanceCounterSill : ISillActivatedByDefault, ISillSingleView
{
    private readonly IPerformanceMonitorService _performanceMonitorService;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IPluginInfo _pluginInfo;

    private PerformanceCounterViewModel? _viewModel;

    [ImportingConstructor]
    internal PerformanceCounterSill(
        IPerformanceMonitorService performanceMonitorService,
        ISettingsProvider settingsProvider,
        IPluginInfo pluginInfo)
    {
        _performanceMonitorService = performanceMonitorService;
        _settingsProvider = settingsProvider;
        _pluginInfo = pluginInfo;

        _viewModel = new PerformanceCounterViewModel(
            _performanceMonitorService,
            _settingsProvider);

        var sillView = new SillView();
        sillView.Content = new PerformanceCounterView(sillView, _pluginInfo, _viewModel);
        View = sillView;
    }

    /// <summary>
    /// Gets the display name of this extension.
    /// </summary>
    public string DisplayName => "/WindowSill.PerfCounter/Misc/DisplayName".GetLocalizedString();

    /// <summary>
    /// Creates the icon for this extension.
    /// </summary>
    public IconElement CreateIcon()
        => new ImageIcon
        {
            Source = new SvgImageSource(new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "microchip.svg")))
        };

    /// <summary>
    /// Gets the settings views for this extension.
    /// </summary>
    public SillSettingsView[]? SettingsViews =>
        [
        new SillSettingsView(
            DisplayName,
            new(() => new SettingsView(_settingsProvider)))
        ];

    /// <summary>
    /// Gets the main view for this extension.
    /// </summary>
    public SillView? View { get; private set; }

    /// <summary>
    /// Called when the extension is activated.
    /// </summary>
    public ValueTask OnActivatedAsync()
    {
        _performanceMonitorService.StartMonitoring();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Called when the extension is deactivated.
    /// </summary>
    public ValueTask OnDeactivatedAsync()
    {
        _performanceMonitorService.StopMonitoring();

        View = null;
        _viewModel = null;

        return ValueTask.CompletedTask;
    }
}
