using CommunityToolkit.Mvvm.ComponentModel;
using WindowSill.API;

namespace WindowSill.Teams.FirstTimeSetup;

internal sealed class TeamsFirstTimeSetupContributor : ObservableObject, IFirstTimeSetupContributor
{
    public bool CanContinue => true;

    public FrameworkElement GetView()
    {
        return new TeamsFirstTimeSetupContributorView();
    }
}
