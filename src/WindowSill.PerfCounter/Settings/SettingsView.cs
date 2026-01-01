using CommunityToolkit.WinUI.Controls;
using WindowSill.API;

namespace WindowSill.PerfCounter.Settings;

internal sealed class SettingsView : UserControl
{
    private readonly SettingsCard _openTaskManagerCard = new();
    private readonly ISettingsProvider _settingsProvider;

    public SettingsView(ISettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;
        this.DataContext(
            new SettingsViewModel(settingsProvider),
            (view, viewModel) => view
            .Content(
                new StackPanel()
                    .Spacing(2)
                    .Children(
                        new TextBlock()
                            .Style(x => x.ThemeResource("BodyStrongTextBlockStyle"))
                            .Margin(0, 0, 0, 8)
                            .Text("/WindowSill.PerfCounter/Settings/General".GetLocalizedString()),

                        new SettingsCard()
                            .Header("/WindowSill.PerfCounter/Settings/DisplayMode".GetLocalizedString())
                            .Description("/WindowSill.PerfCounter/Settings/DisplayModeDescription".GetLocalizedString())
                            .HeaderIcon(
                                new FontIcon()
                                    .Glyph("\uE7C4")
                            )
                            .Content(
                                new ComboBox()
                                    .SelectedItem(x => x.Binding(() => viewModel.SelectedDisplayModeItem)
                                                        .TwoWay()
                                                        .UpdateSourceTrigger(UpdateSourceTrigger.PropertyChanged))
                                    .ItemsSource(x => x.Binding(() => viewModel.AvailableDisplayModeItems))
                                    .Width(150)
                            ),

                        new SettingsCard()
                            .Header("/WindowSill.PerfCounter/Settings/AnimationMetric".GetLocalizedString())
                            .Description("/WindowSill.PerfCounter/Settings/AnimationMetricDescription".GetLocalizedString())
                            .Visibility(x => x.Binding(() => viewModel.IsAnimatedGifMode)
                                              .OneWay()
                                              .Convert(isAnimated => isAnimated ? Visibility.Visible : Visibility.Collapsed))
                            .HeaderIcon(
                                new FontIcon()
                                    .Glyph("\uE768")
                            )
                            .Content(
                                new ComboBox()
                                    .SelectedItem(x => x.Binding(() => viewModel.AnimationMetric)
                                                        .TwoWay()
                                                        .UpdateSourceTrigger(UpdateSourceTrigger.PropertyChanged))
                                    .ItemsSource(x => x.Binding(() => viewModel.AvailableMetrics))
                                    .Width(150)
                            ),

                        new SettingsCard()
                            .Header("/WindowSill.PerfCounter/Settings/EnableTaskManagerLaunch".GetLocalizedString())
                            .Description("/WindowSill.PerfCounter/Settings/EnableTaskManagerLaunchDescription".GetLocalizedString())
                            .HeaderIcon(
                                new FontIcon()
                                    .Glyph("\uE7EF")
                            )
                            .Content(
                                new ToggleSwitch()
                                    .IsOn(x => x.Binding(() => viewModel.EnableTaskManagerLaunch)
                                                .TwoWay()
                                                .UpdateSourceTrigger(UpdateSourceTrigger.PropertyChanged))
                            ),

                        _openTaskManagerCard
                            .Header("/WindowSill.PerfCounter/Settings/OpenTaskManager".GetLocalizedString())
                            .Description("/WindowSill.PerfCounter/Settings/OpenTaskManagerDescription".GetLocalizedString())
                            .HeaderIcon(
                                new FontIcon()
                                    .Glyph("\uE7EF")
                            )
                            .ActionIcon(
                                new FontIcon()
                                    .Glyph("\uE8A7")
                            )
                            .IsClickEnabled(true)
                    )
            )
        );

        _openTaskManagerCard.Click += OpenTaskManagerCard_Click;
    }

    private void OpenTaskManagerCard_Click(object sender, RoutedEventArgs e)
    {
        TaskManagerLauncher.OpenTaskManager(_settingsProvider);
    }
}
