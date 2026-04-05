using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Media.Animation;
using WindowSill.API;
using WindowSill.Terminal.Core.Commands;
using WindowSill.Terminal.Messages;
using WindowSill.Terminal.ViewModels;

namespace WindowSill.Terminal.Views;

internal sealed partial class CommandPopup : SillPopupContent, IRecipient<CommandPopupDismissMessage>
{
    private readonly IMessenger _messenger;

    internal CommandPopup(IMessenger messenger, CommandViewModel viewModel)
    {
        _messenger = messenger;
        ViewModel = viewModel;
        InitializeComponent();
    }

    internal CommandViewModel ViewModel { get; }

    public void Receive(CommandPopupDismissMessage message)
    {
        Close();
    }

    public override void OnOpening()
    {
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        _messenger.RegisterAll(this);

        switch (ViewModel.State)
        {
            case CommandState.Pending:
                ContentFrame.Navigate(typeof(CommandPopupConfigurePage), ViewModel);
                break;

            case CommandState.Running:
                ContentFrame.Navigate(typeof(CommandPopupExecutionPage), ViewModel);
                break;

            default:
                ContentFrame.Navigate(typeof(CommandPopupResultPage), ViewModel);
                break;
        }
    }

    public override void OnClosing()
    {
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        ViewModel.Dispose();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.State))
        {
            switch (ViewModel.State)
            {
                case CommandState.Pending:
                    ContentFrame.Navigate(
                        typeof(CommandPopupConfigurePage),
                        ViewModel,
                        new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft });
                    break;

                case CommandState.Running:
                    ContentFrame.Navigate(
                        typeof(CommandPopupExecutionPage),
                        ViewModel,
                        new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
                    break;

                default:
                    ContentFrame.Navigate(
                        typeof(CommandPopupResultPage),
                        ViewModel,
                        new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
                    break;
            }
        }
    }
}
