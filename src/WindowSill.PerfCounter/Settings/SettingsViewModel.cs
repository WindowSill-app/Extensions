using CommunityToolkit.Mvvm.ComponentModel;
using WindowSill.API;
using System.Collections.ObjectModel;

namespace WindowSill.PerfCounter.Settings;

internal sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsProvider _settingsProvider;

    public SettingsViewModel(ISettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;
    }

    public PerformanceDisplayMode DisplayMode
    {
        get => _settingsProvider.GetSetting(Settings.DisplayMode);
        set
        {
            _settingsProvider.SetSetting(Settings.DisplayMode, value);
            OnPropertyChanged(nameof(IsAnimatedGifMode));
            OnPropertyChanged(nameof(SelectedDisplayModeItem));
        }
    }

    public DisplayModeItem? SelectedDisplayModeItem
    {
        get => AvailableDisplayModeItems.FirstOrDefault(item => item.Value == DisplayMode);
        set
        {
            if (value != null)
            {
                DisplayMode = value.Value;
            }
        }
    }

    public PerformanceMetric AnimationMetric
    {
        get => _settingsProvider.GetSetting(Settings.AnimationMetric);
        set => _settingsProvider.SetSetting(Settings.AnimationMetric, value);
    }

    public bool EnableTaskManagerLaunch
    {
        get => _settingsProvider.GetSetting(Settings.EnableTaskManagerLaunch);
        set => _settingsProvider.SetSetting(Settings.EnableTaskManagerLaunch, value);
    }

    public bool IsAnimatedGifMode => DisplayMode == PerformanceDisplayMode.RunningMan;

    public ObservableCollection<DisplayModeItem> AvailableDisplayModeItems { get; } =
    [
        new DisplayModeItem(PerformanceDisplayMode.Percentage, "/WindowSill.PerfCounter/Settings/DisplayModePercentage".GetLocalizedString()),
        new DisplayModeItem(PerformanceDisplayMode.RunningMan, "/WindowSill.PerfCounter/Settings/DisplayModeRunningMan".GetLocalizedString())
    ];

    public ObservableCollection<PerformanceMetric> AvailableMetrics { get; } =
    [
        PerformanceMetric.CPU,
        PerformanceMetric.GPU,
        PerformanceMetric.RAM
    ];
}
