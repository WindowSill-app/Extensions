using WindowSill.InlineTerminal.ViewModels;

namespace WindowSill.InlineTerminal.Views;

public sealed partial class OnGoingCommandsSillContent : UserControl
{
    internal OnGoingCommandsSillContent(OnGoingCommandsViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }

    internal OnGoingCommandsViewModel ViewModel { get; }
}
