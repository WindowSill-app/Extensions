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

        UpdateAnimationMetricVisibility();
        UpdatePercentageOptionsVisibility();

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.IsAnimatedGifMode))
        {
            UpdateAnimationMetricVisibility();
        }

        if (e.PropertyName == nameof(SettingsViewModel.IsPercentageMode))
        {
            UpdatePercentageOptionsVisibility();
        }
    }

    private void UpdateAnimationMetricVisibility()
    {
        AnimationMetricCard.Visibility = ViewModel.IsAnimatedGifMode ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdatePercentageOptionsVisibility()
    {
        Visibility vis = ViewModel.IsPercentageMode ? Visibility.Visible : Visibility.Collapsed;
        ShowCpuCard.Visibility = vis;
        ShowGpuCard.Visibility = vis;
        ShowRamCard.Visibility = vis;
    }

    private void OpenTaskManagerCard_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenTaskManagerCommand.Execute(null);
    }
}
