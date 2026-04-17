using WindowSill.InlineTerminal.ViewModels;

namespace WindowSill.InlineTerminal.Views;

internal sealed partial class CommandSillContent : UserControl
{
    internal CommandSillContent(CommandViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    internal CommandViewModel ViewModel { get; }
}
