using Microsoft.UI.Xaml.Media.Imaging;

using System.ComponentModel.Composition;

using WindowSill.API;
using WindowSill.PerfCounter.Services;
using WindowSill.PerfCounter.Settings;
using WindowSill.PerfCounter.UI;

namespace WindowSill.PerfCounter;

[Export(typeof(ISill))]
[Name("Performance Counter")]
[Priority(Priority.Lowest)]
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

        // Create the performance counter view
        (PerformanceCounterViewModel? viewModel, SillView? perfView) = PerformanceCounterViewModel.CreateView(
            _performanceMonitorService,
            _settingsProvider,
            _pluginInfo);

        _viewModel = viewModel;
        View = perfView;
    }

    public string DisplayName => "/WindowSill.PerfCounter/Misc/DisplayName".GetLocalizedString();

    public IconElement CreateIcon()
        => new ImageIcon
        {
            Source = new SvgImageSource(new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "microchip.svg")))
        };

    public SillSettingsView[]? SettingsViews =>
        [
        new SillSettingsView(
            DisplayName,
            new(() => new SettingsView(_settingsProvider)))
        ];

    public SillView? View { get; private set; }

    public ValueTask OnActivatedAsync()
    {
        // Start monitoring performance
        _performanceMonitorService.StartMonitoring();
        return ValueTask.CompletedTask;
    }

    public ValueTask OnDeactivatedAsync()
    {
        // Stop monitoring performance
        _performanceMonitorService.StopMonitoring();

        View = null;
        _viewModel = null;

        return ValueTask.CompletedTask;
    }
}