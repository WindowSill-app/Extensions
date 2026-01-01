using CommunityToolkit.WinUI.Controls;
using Windows.System;
using WindowSill.API;

namespace WindowSill.Teams.Settings;

internal sealed class SettingsView : UserControl
{
    public SettingsView()
    {
        var settingsCard = new SettingsCard();
        settingsCard.Click += SettingsCard_ClickAsync;
        this.Content(
            new StackPanel()
                .Spacing(2)
                .Children(
                    new TextBlock()
                        .Style(x => x.ThemeResource("BodyStrongTextBlockStyle"))
                        .Margin(0, 0, 0, 8)
                        .Text("/WindowSill.Teams/SettingsView/General".GetLocalizedString()),
                    settingsCard
                        .Header("/WindowSill.Teams/SettingsView/Connect/Header".GetLocalizedString())
                        .Description("/WindowSill.Teams/SettingsView/Connect/Description".GetLocalizedString())
                        .HeaderIcon(
                            new FontIcon()
                                .Glyph("\uE774")
                                .FontFamily("Segoe Fluent Icons,Segoe MDL2 Assets"))
                        .ActionIcon(
                            new FontIcon()
                                .Glyph("\uE8A7")
                                .FontFamily("Segoe Fluent Icons,Segoe MDL2 Assets"))
                        .IsClickEnabled(true)
                        .Content(
                            new HyperlinkButton()
                                .Content("/WindowSill.Teams/SettingsView/ConnectGuide".GetLocalizedString())
                                .NavigateUri(new Uri("https://support.microsoft.com/en-us/office/connect-to-third-party-devices-in-microsoft-teams-aabca9f2-47bb-407f-9f9b-81a104a883d6?storagetype=live"))
                        )
                )
        );
    }

    private async void SettingsCard_ClickAsync(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://support.microsoft.com/en-us/office/connect-to-third-party-devices-in-microsoft-teams-aabca9f2-47bb-407f-9f9b-81a104a883d6?storagetype=live"));
    }
}
