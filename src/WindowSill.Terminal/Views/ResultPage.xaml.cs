using WindowSill.Terminal.ViewModels;

namespace WindowSill.Terminal.Views;

/// <summary>
/// Page displaying the final result of a command execution.
/// </summary>
internal sealed partial class ResultPage : Page
{
    public ResultPage()
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
