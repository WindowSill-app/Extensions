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
}
