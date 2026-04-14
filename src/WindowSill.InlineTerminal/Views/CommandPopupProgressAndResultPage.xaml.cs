using WindowSill.API;
using WindowSill.InlineTerminal.ViewModels;

namespace WindowSill.InlineTerminal.Views;

public sealed partial class CommandPopupProgressAndResultPage : Page
{
    public CommandPopupProgressAndResultPage()
    {
        InitializeComponent();
    }

    internal CommandViewModel ViewModel { get; private set; } = null!;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel = (CommandViewModel)e.Parameter;
    }

    private void DismissFlyout_Opening(object sender, object e)
    {
        DismissFlyout.Items.Clear();

        if (ViewModel.HasOtherRuns)
        {
            DismissFlyout.Items.Add(new MenuFlyoutItem
            {
                Text = string.Format(
                    "/WindowSill.InlineTerminal/CommandPopupResultPage/DismissOthersFormat".GetLocalizedString(),
                    ViewModel.OtherRunsCount),
                Command = ViewModel.DismissOthersCommand,
            });
        }

        if (ViewModel.RunCount > 1)
        {
            DismissFlyout.Items.Add(new MenuFlyoutItem
            {
                Text = string.Format(
                    "/WindowSill.InlineTerminal/CommandPopupResultPage/DismissAllFormat".GetLocalizedString(),
                    ViewModel.RunCount),
                Command = ViewModel.DismissAllCommand,
            });
        }

        if (DismissFlyout.Items.Count == 0)
        {
            DismissFlyout.Items.Add(new MenuFlyoutItem
            {
                Text = "/WindowSill.InlineTerminal/CommandPopupResultPage/NoOtherRuns".GetLocalizedString(),
                IsEnabled = false,
            });
        }
    }
}
