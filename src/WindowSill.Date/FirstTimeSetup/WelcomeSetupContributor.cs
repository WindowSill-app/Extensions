using CommunityToolkit.Mvvm.ComponentModel;
using WindowSill.API;

namespace WindowSill.Date.FirstTimeSetup;

/// <summary>
/// First-time setup step that introduces the Date extension to the user.
/// </summary>
internal sealed class WelcomeSetupContributor : ObservableObject, IFirstTimeSetupContributor
{
    /// <inheritdoc/>
    public bool CanContinue => true;

    /// <inheritdoc/>
    public FrameworkElement GetView()
    {
        return new WelcomeSetupContributorView();
    }
}
