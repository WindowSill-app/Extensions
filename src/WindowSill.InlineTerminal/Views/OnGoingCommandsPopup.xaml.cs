using WindowSill.API;
using WindowSill.InlineTerminal.Core.Shell;
using WindowSill.InlineTerminal.Services;
using WindowSill.InlineTerminal.ViewModels;

namespace WindowSill.InlineTerminal.Views;

public sealed partial class OnGoingCommandsPopup : SillPopupContent
{
    private readonly CommandService _commandService;
    private readonly IReadOnlyList<ShellInfo> _availableShells;
    private readonly ISettingsProvider _settingsProvider;
    private readonly SillViewBase _sillView;

    internal OnGoingCommandsPopup(
        CommandService commandService,
        IReadOnlyList<ShellInfo> availableShells,
        ISettingsProvider settingsProvider,
        OnGoingCommandsViewModel viewModel,
        SillViewBase sillView)
    {
        _commandService = commandService;
        _availableShells = availableShells;
        _settingsProvider = settingsProvider;
        _sillView = sillView;
        ViewModel = viewModel;
        InitializeComponent();
    }

    internal OnGoingCommandsViewModel ViewModel { get; }

    private void ShortcutListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ActiveRunItem item)
        {
            OpenCommandPopup(item);
        }
    }

    private void ShortcutListView_ItemInvoked(object sender, ActiveRunItem e)
    {
        OpenCommandPopup(e);
    }

    private void DismissButton_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is ActiveRunItem item)
        {
            _commandService.DismissRun(item.Command.Id, item.Run.Id);
        }
    }

    private void DismissAllButton_Click(object sender, RoutedEventArgs e)
    {
        _commandService.DismissAllCommands();
        Close();
    }

    private void Item_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is ActiveRunItem item)
        {
            var viewModel = new CommandViewModel(_commandService, item.Command, _availableShells, _settingsProvider);
            var menuFlyout = new MenuFlyout();
            MenuFlyoutBuilder.PopulateMenu(menuFlyout, item.Command, viewModel, hasSelectedText: false, _commandService);
            menuFlyout.ShowAt((FrameworkElement)sender, e.GetPosition((UIElement)sender));
        }
    }

    private void OpenCommandPopup(ActiveRunItem item)
    {
        var viewModel = new CommandViewModel(_commandService, item.Command, _availableShells, _settingsProvider);
        var sillPopup = new SillPopup();
        sillPopup.Content = new CommandPopup(viewModel);

        Close();
        sillPopup
            .ShowAsync(_sillView)
            .ContinueWith((_) =>
            {
                ThreadHelper.RunOnUIThreadAsync(() =>
                {
                    ((IDisposable)sillPopup.Content).Dispose();
                });
            })
            .ForgetSafely();
    }
}
