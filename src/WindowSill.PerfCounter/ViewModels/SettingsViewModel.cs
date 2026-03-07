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
    /// Gets or sets whether clicking the counter opens Task Manager.
    /// </summary>
    public bool EnableTaskManagerLaunch
    {
        get => _settingsProvider.GetSetting(Settings.Settings.EnableTaskManagerLaunch);
        set => _settingsProvider.SetSetting(Settings.Settings.EnableTaskManagerLaunch, value);
    }

    /// <summary>
    /// Gets or sets whether to display CPU and GPU temperature.
    /// </summary>
    public bool ShowTemperature
    {
        get => _settingsProvider.GetSetting(Settings.Settings.ShowTemperature);
        set => _settingsProvider.SetSetting(Settings.Settings.ShowTemperature, value);
    }

    /// <summary>
    /// Gets whether the animated running man mode is active.
    /// </summary>
    public bool IsAnimatedGifMode => DisplayMode == PerformanceDisplayMode.RunningMan;

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
        TaskManagerLauncher.OpenTaskManager(_settingsProvider);
    }
}
