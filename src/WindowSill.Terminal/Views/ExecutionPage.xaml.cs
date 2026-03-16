using WindowSill.Terminal.ViewModels;

namespace WindowSill.Terminal.Views;

/// <summary>
/// Page displaying live command execution output.
/// </summary>
internal sealed partial class ExecutionPage : Page
{
    public ExecutionPage()
    {
        InitializeComponent();
    }

    internal CommandItemViewModel ViewModel { get; private set; } = null!;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel = (CommandItemViewModel)e.Parameter;
    }
}
