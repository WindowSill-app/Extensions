using CommunityToolkit.WinUI.Controls;

using WindowSill.API;

namespace WindowSill.MediaControl.Settings;

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
                            .Text("/WindowSill.MediaControl/SettingsUserControl/General".GetLocalizedString()),
                        new SettingsCard()
                            .Header("/WindowSill.MediaControl/SettingsUserControl/ShowMediaNameAndThumbnail".GetLocalizedString())
                            .HeaderIcon(
                                new FontIcon()
                                    .Glyph("\uE8B9")
                                    .FontFamily("Segoe Fluent Icons,Segoe MDL2 Assets"))
                            .Content(
                                new ToggleSwitch()
                                    .IsOn(
                                        x => x.Binding(() => viewModel.ShowTitleArtistAndThumbnail)
                                              .TwoWay()
                                              .UpdateSourceTrigger(UpdateSourceTrigger.PropertyChanged)
                                    )
                            )
                    )
            )
        );
    }
}
