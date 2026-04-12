using Microsoft.UI.Xaml.Media.Animation;
using WindowSill.API;
using WindowSill.InlineTerminal.Core.Commands;
using WindowSill.InlineTerminal.ViewModels;

namespace WindowSill.InlineTerminal.Views;

internal sealed partial class CommandPopup : SillPopupContent, IDisposable
{
    private bool _isOnCommandPopupProgressAndResultPage;

    internal CommandPopup(CommandViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    internal CommandViewModel ViewModel { get; }

    public void Dispose()
    {
        ViewModel.Dispose();
    }

    public override void OnOpening()
    {
        ViewModel.RequestClose += ViewModel_RequestClose;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        switch (ViewModel.State)
        {
            case CommandState.Created:
                ContentFrame.Navigate(typeof(CommandPopupConfigurePage), ViewModel);
                break;

            default:
                _isOnCommandPopupProgressAndResultPage = true;
                ContentFrame.Navigate(typeof(CommandPopupProgressAndResultPage), ViewModel);
                break;
        }
    }

    public override void OnClosing()
    {
        ViewModel.RequestClose -= ViewModel_RequestClose;
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }

    private void ViewModel_RequestClose(object? sender, EventArgs e)
    {
        Close();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.State))
        {
            switch (ViewModel.State)
            {
                case CommandState.Created:
                    _isOnCommandPopupProgressAndResultPage = false;
                    ContentFrame.Navigate(
                        typeof(CommandPopupConfigurePage),
                        ViewModel,
                        new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft });
                    break;

                default:
                    if (!_isOnCommandPopupProgressAndResultPage)
                    {
                        ContentFrame.Navigate(
                            typeof(CommandPopupProgressAndResultPage),
                            ViewModel,
                            new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
                        _isOnCommandPopupProgressAndResultPage = true;
                    }
                    break;
            }
        }
    }
}
