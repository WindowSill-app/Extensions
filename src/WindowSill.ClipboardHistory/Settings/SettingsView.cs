using CommunityToolkit.WinUI.Controls;
using Windows.System;
using WindowSill.API;

namespace WindowSill.ClipboardHistory.Settings;

internal sealed class SettingsView : UserControl
{
    private readonly SettingsCard _openWindowsClipboardHistorySettingsCard = new();
    private readonly SettingsCard _enableClipboardHistoryCard = new();

    public SettingsView(ISettingsProvider settingsProvider)
    {
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
                            .Text("/WindowSill.ClipboardHistory/SettingsUserControl/General".GetLocalizedString()),

                        new SettingsCard()
                            .Header("/WindowSill.ClipboardHistory/SettingsUserControl/MaxItemNumber/Header".GetLocalizedString())
                            .Description("/WindowSill.ClipboardHistory/SettingsUserControl/MaxItemNumber/Description".GetLocalizedString())
                            .HeaderIcon(
                                new FontIcon()
                                    .Glyph("\u0023")
                                    .FontFamily("Segoe UI")
                            )
                            .Content(
                                new NumberBox()
                                    .Value(x => x.Binding(() => viewModel.MaximumHistoryCount)
                                                  .TwoWay()
                                                  .UpdateSourceTrigger(UpdateSourceTrigger.PropertyChanged))
                                    .Minimum(1)
                                    .Maximum(25)
                                    .Width(150)
                                    .SpinButtonPlacementMode(NumberBoxSpinButtonPlacementMode.Inline)
                            ),

                        new SettingsCard()
                            .Header("/WindowSill.ClipboardHistory/SettingsUserControl/HidePasswords/Header".GetLocalizedString())
                            .Description("/WindowSill.ClipboardHistory/SettingsUserControl/HidePasswords/Description".GetLocalizedString())
                            .HeaderIcon(
                                new FontIcon()
                                    .Glyph("\uE9A9")
                            )
                            .Content(
                                new ToggleSwitch()
                                    .IsOn(
                                        x => x.Binding(() => viewModel.HidePasswords)
                                              .TwoWay()
                                              .UpdateSourceTrigger(UpdateSourceTrigger.PropertyChanged)
                                    )
                            ),

                        _openWindowsClipboardHistorySettingsCard
                            .Header("/WindowSill.ClipboardHistory/SettingsUserControl/OpenWindowsClipboardHistorySettings".GetLocalizedString())
                            .Visibility(x => x.Binding(() => viewModel.IsClipboardHistoryEnabled)
                                              .OneWay()
                                              .Convert(isEnabled => isEnabled ? Visibility.Visible : Visibility.Collapsed))
                            .HeaderIcon(
                                new FontIcon()
                                    .Glyph("\uE713")
                            )
                            .ActionIcon(
                                new FontIcon()
                                    .Glyph("\uE8A7")
                            )
                            .IsClickEnabled(true),

                        _enableClipboardHistoryCard
                            .Header("/WindowSill.ClipboardHistory/SettingsUserControl/EnableClipboardHistory".GetLocalizedString())
                            .Visibility(x => x.Binding(() => viewModel.IsClipboardHistoryEnabled)
                                              .OneWay()
                                              .Convert(isEnabled => !isEnabled ? Visibility.Visible : Visibility.Collapsed))
                            .HeaderIcon(
                                new FontIcon()
                                    .Glyph("\uE713")
                            )
                            .ActionIcon(
                                new FontIcon()
                                    .Glyph("\uE8A7")
                            )
                            .IsClickEnabled(true)
                    )
            )
        );

        _openWindowsClipboardHistorySettingsCard.Click += OpenWindowsClipboardHistorySettingsCard_Click;
        _enableClipboardHistoryCard.Click += OpenWindowsClipboardHistorySettingsCard_Click;
    }

    private async void OpenWindowsClipboardHistorySettingsCard_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("ms-settings:clipboard"));
    }
}
