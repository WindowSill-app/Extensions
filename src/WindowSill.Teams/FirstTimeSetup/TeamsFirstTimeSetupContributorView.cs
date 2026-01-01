using CommunityToolkit.WinUI.Controls;
using WindowSill.API;

namespace WindowSill.Teams.FirstTimeSetup;

internal sealed class TeamsFirstTimeSetupContributorView : UserControl
{
    public TeamsFirstTimeSetupContributorView()
    {
        this.Content(
            new StackPanel()
                .Spacing(16)
                .Children(
                    new TextBlock()
                        .TextWrapping(TextWrapping.WrapWholeWords)
                        .Text("/WindowSill.Teams/TeamsFirstTimeSetupContributorView/Intro".GetLocalizedString()),
                    new HyperlinkButton()
                        .Padding(0)
                        .Content("/WindowSill.Teams/TeamsFirstTimeSetupContributorView/ConnectGuide".GetLocalizedString())
                        .NavigateUri(new Uri("https://support.microsoft.com/en-us/office/connect-to-third-party-devices-in-microsoft-teams-aabca9f2-47bb-407f-9f9b-81a104a883d6?storagetype=live"))
                )
        );
    }
}
