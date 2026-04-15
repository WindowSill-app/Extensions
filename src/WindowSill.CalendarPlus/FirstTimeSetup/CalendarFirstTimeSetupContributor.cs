using CommunityToolkit.Mvvm.ComponentModel;

using WindowSill.API;

namespace WindowSill.CalendarPlus.FirstTimeSetup;

/// <summary>
/// First-time setup contributor for Calendar Plus account configuration.
/// </summary>
internal sealed class CalendarFirstTimeSetupContributor : ObservableObject, IFirstTimeSetupContributor
{
    /// <inheritdoc/>
    public bool CanContinue => true;

    /// <inheritdoc/>
    public FrameworkElement GetView()
    {
        return new CalendarFirstTimeSetupView();
    }
}
