using CommunityToolkit.Mvvm.ComponentModel;
using WindowSill.API;

namespace WindowSill.ClipboardHistory.FirstTimeSetup;

internal sealed partial class ClipboardHistoryFirstTimeSetupContributor : ObservableObject, IFirstTimeSetupContributor
{
    [ObservableProperty]
    public partial bool CanContinue { get; set; } = true;

    public FrameworkElement GetView()
    {
        return new ClipboardHistoryFirstTimeSetupContributorView(this);
    }
}
