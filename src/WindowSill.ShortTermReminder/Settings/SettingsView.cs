using CommunityToolkit.WinUI.Controls;

using WindowSill.API;

namespace WindowSill.ShortTermReminder.Settings;

internal sealed class SettingsView : UserControl
{
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
                            .Text("/WindowSill.ShortTermReminder/Settings/General".GetLocalizedString()),

                        new SettingsCard()
                            .Header("/WindowSill.ShortTermReminder/Settings/NotificationMode/Header".GetLocalizedString())
                            .Description("/WindowSill.ShortTermReminder/Settings/NotificationMode/Description".GetLocalizedString())
                            .HeaderIcon(
                                new FontIcon()
                                    .Glyph("\uE7E7")
                            )
                            .Content(
                                new ToggleSwitch()
                                    .OnContent("/WindowSill.ShortTermReminder/Settings/NotificationModeToggle/OnContent".GetLocalizedString())
                                    .OffContent("/WindowSill.ShortTermReminder/Settings/NotificationModeToggle/OffContent".GetLocalizedString())
                                    .IsOn(
                                        x => x.Binding(() => viewModel.UseFullScreenNotification)
                                              .TwoWay()
                                              .UpdateSourceTrigger(UpdateSourceTrigger.PropertyChanged)
                                    )
                            )
                    )
            )
        );
    }
}
