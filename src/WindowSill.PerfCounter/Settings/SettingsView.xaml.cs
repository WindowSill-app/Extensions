using WindowSill.API;
using WindowSill.PerfCounter.ViewModels;

namespace WindowSill.PerfCounter.Settings;

/// <summary>
/// Settings page for the PerfCounter extension.
/// </summary>
internal sealed partial class SettingsView : UserControl
{
    /// <summary>
    /// Gets the ViewModel for data binding.
    /// </summary>
    internal SettingsViewModel ViewModel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsView"/> class.
    /// </summary>
    /// <param name="settingsProvider">The settings provider for user preferences.</param>
    public SettingsView(ISettingsProvider settingsProvider)
    {
        ViewModel = new SettingsViewModel(settingsProvider);

        InitializeComponent();

        SetLocalizedStrings();
        UpdateAnimationMetricVisibility();

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void SetLocalizedStrings()
    {
        GeneralHeader.Text = "/WindowSill.PerfCounter/Settings/General".GetLocalizedString();

        DisplayModeCard.Header = "/WindowSill.PerfCounter/Settings/DisplayMode".GetLocalizedString();
        DisplayModeCard.Description = "/WindowSill.PerfCounter/Settings/DisplayModeDescription".GetLocalizedString();

        AnimationMetricCard.Header = "/WindowSill.PerfCounter/Settings/AnimationMetric".GetLocalizedString();
        AnimationMetricCard.Description = "/WindowSill.PerfCounter/Settings/AnimationMetricDescription".GetLocalizedString();

        EnableTaskManagerCard.Header = "/WindowSill.PerfCounter/Settings/EnableTaskManagerLaunch".GetLocalizedString();
        EnableTaskManagerCard.Description = "/WindowSill.PerfCounter/Settings/EnableTaskManagerLaunchDescription".GetLocalizedString();

        ShowTemperatureCard.Header = "/WindowSill.PerfCounter/Settings/ShowTemperature".GetLocalizedString();
        ShowTemperatureCard.Description = "/WindowSill.PerfCounter/Settings/ShowTemperatureDescription".GetLocalizedString();

        OpenTaskManagerCard.Header = "/WindowSill.PerfCounter/Settings/OpenTaskManager".GetLocalizedString();
        OpenTaskManagerCard.Description = "/WindowSill.PerfCounter/Settings/OpenTaskManagerDescription".GetLocalizedString();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.IsAnimatedGifMode))
        {
            UpdateAnimationMetricVisibility();
        }
    }

    private void UpdateAnimationMetricVisibility()
    {
        AnimationMetricCard.Visibility = ViewModel.IsAnimatedGifMode ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OpenTaskManagerCard_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenTaskManagerCommand.Execute(null);
    }
}
