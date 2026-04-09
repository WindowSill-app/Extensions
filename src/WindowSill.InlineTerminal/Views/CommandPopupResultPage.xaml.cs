using WindowSill.InlineTerminal.ViewModels;

namespace WindowSill.InlineTerminal.Views;

public sealed partial class CommandPopupResultPage : Page
{
    public CommandPopupResultPage()
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
