using Microsoft.UI.Xaml.Media.Animation;
using WindowSill.API;
using WindowSill.Terminal.ViewModels;

namespace WindowSill.Terminal.Views;

/// <summary>
/// Popup container that uses Frame navigation between Configure, Execution, and Result pages.
/// </summary>
internal sealed partial class TerminalPopup : SillPopupContent
{
    internal TerminalPopup(CommandItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        ViewModel.ExecutionStarted += OnExecutionStarted;
        ViewModel.ExecutionCompleted += OnExecutionCompleted;
        ViewModel.RerunRequested += OnRerunRequested;
        ViewModel.DismissRequested += OnDismissRequested;
    }

    internal CommandItemViewModel ViewModel { get; }

    private void SillPopupContent_Opening(object sender, EventArgs e)
    {
        switch (ViewModel.State)
        {
            case Core.CommandState.Pending:
                ContentFrame.Navigate(typeof(ConfigurePage), ViewModel);
                break;
            case Core.CommandState.Running:
                ContentFrame.Navigate(typeof(ExecutionPage), ViewModel);
                break;
            default:
                ContentFrame.Navigate(typeof(ResultPage), ViewModel);
                break;
        }
    }

    private void OnExecutionStarted(object? sender, EventArgs e)
    {
        ContentFrame.Navigate(
            typeof(ExecutionPage),
            ViewModel,
            new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
    }

    private void OnExecutionCompleted(object? sender, EventArgs e)
    {
        ContentFrame.Navigate(
            typeof(ResultPage),
            ViewModel,
            new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
    }

    private void OnRerunRequested(object? sender, EventArgs e)
    {
        ContentFrame.Navigate(
            typeof(ExecutionPage),
            ViewModel,
            new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
    }

    private void OnDismissRequested(object? sender, EventArgs e)
    {
        Close();
    }
}
