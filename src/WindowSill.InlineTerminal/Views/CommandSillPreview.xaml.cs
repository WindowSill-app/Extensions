using WindowSill.InlineTerminal.ViewModels;

namespace WindowSill.InlineTerminal.Views;

internal sealed partial class CommandSillPreview : UserControl
{
    internal CommandSillPreview(CommandViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    internal CommandViewModel ViewModel { get; }
}
