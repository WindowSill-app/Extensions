using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System.Collections.ObjectModel;

using WindowSill.API;
using WindowSill.PerfCounter.Settings;

namespace WindowSill.PerfCounter.ViewModels;

/// <summary>
/// ViewModel for the PerfCounter settings page.
/// </summary>
internal sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsProvider _settingsProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    /// <param name="settingsProvider">The settings provider for persisting user preferences.</param>
    public SettingsViewModel(ISettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;
    }

    /// <summary>
    /// Gets or sets the current display mode.
    /// </summary>
    public PerformanceDisplayMode DisplayMode
    {
        get => _settingsProvider.GetSetting(Settings.Settings.DisplayMode);
        set
        {
            _settingsProvider.SetSetting(Settings.Settings.DisplayMode, value);
            OnPropertyChanged(nameof(IsAnimatedGifMode));
            OnPropertyChanged(nameof(IsPercentageMode));
            OnPropertyChanged(nameof(SelectedDisplayModeItem));
        }
    }

    /// <summary>
    /// Gets or sets the selected display mode item for the ComboBox.
    /// </summary>
    public DisplayModeItem? SelectedDisplayModeItem
    {
        get => AvailableDisplayModeItems.FirstOrDefault(item => item.Value == DisplayMode);
        set
        {
            if (value != null && DisplayMode != value.Value)
            {
                DisplayMode = value.Value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the metric used for animation speed.
    /// </summary>
    public PerformanceMetric AnimationMetric
    {
        get => _settingsProvider.GetSetting(Settings.Settings.AnimationMetric);
        set => _settingsProvider.SetSetting(Settings.Settings.AnimationMetric, value);
    }

    /// <summary>
    /// Gets or sets whether to show CPU usage in percentage mode.
    /// </summary>
    public bool ShowCpu
    {
        get => _settingsProvider.GetSetting(Settings.Settings.ShowCpu);
        set => _settingsProvider.SetSetting(Settings.Settings.ShowCpu, value);
    }

    /// <summary>
    /// Gets or sets whether to show GPU usage in percentage mode.
    /// </summary>
    public bool ShowGpu
    {
        get => _settingsProvider.GetSetting(Settings.Settings.ShowGpu);
        set => _settingsProvider.SetSetting(Settings.Settings.ShowGpu, value);
    }

    /// <summary>
    /// Gets or sets whether to show RAM usage in percentage mode.
    /// </summary>
    public bool ShowRam
    {
        get => _settingsProvider.GetSetting(Settings.Settings.ShowRam);
        set => _settingsProvider.SetSetting(Settings.Settings.ShowRam, value);
    }

    /// <summary>
    /// Gets or sets whether to show CPU temperature in percentage mode.
    /// </summary>
    public bool ShowCpuTemperature
    {
        get => _settingsProvider.GetSetting(Settings.Settings.ShowCpuTemperature);
        set => _settingsProvider.SetSetting(Settings.Settings.ShowCpuTemperature, value);
    }

    /// <summary>
    /// Gets or sets whether to show GPU temperature in percentage mode.
    /// </summary>
    public bool ShowGpuTemperature
    {
        get => _settingsProvider.GetSetting(Settings.Settings.ShowGpuTemperature);
        set => _settingsProvider.SetSetting(Settings.Settings.ShowGpuTemperature, value);
    }

    /// <summary>
    /// Gets whether the animated running man mode is active.
    /// </summary>
    public bool IsAnimatedGifMode => DisplayMode == PerformanceDisplayMode.RunningMan;

    /// <summary>
    /// Gets whether percentage display mode is active.
    /// </summary>
    public bool IsPercentageMode => DisplayMode == PerformanceDisplayMode.Percentage;

    /// <summary>
    /// Gets the available display mode options.
    /// </summary>
    public ObservableCollection<DisplayModeItem> AvailableDisplayModeItems { get; } =
    [
        new DisplayModeItem(PerformanceDisplayMode.Percentage, "/WindowSill.PerfCounter/Settings/DisplayModePercentage".GetLocalizedString()),
        new DisplayModeItem(PerformanceDisplayMode.RunningMan, "/WindowSill.PerfCounter/Settings/DisplayModeRunningMan".GetLocalizedString())
    ];

    /// <summary>
    /// Gets the available performance metric options.
    /// </summary>
    public ObservableCollection<PerformanceMetric> AvailableMetrics { get; } =
    [
        PerformanceMetric.CPU,
        PerformanceMetric.GPU,
        PerformanceMetric.RAM
    ];

    /// <summary>
    /// Opens the Windows Task Manager.
    /// </summary>
    [RelayCommand]
    private void OpenTaskManager()
    {
        TaskManagerLauncher.OpenTaskManager();
    }
}
