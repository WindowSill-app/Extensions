using WindowSill.API;
using WindowSill.InlineTerminal.Core.Commands;
using WindowSill.InlineTerminal.Core.Shell;
using WindowSill.InlineTerminal.ViewModels;

namespace WindowSill.InlineTerminal.Views;

public sealed partial class OnGoingCommandsPopup : SillPopupContent
{
    private readonly CommandExecutionService _commandExecutionService;
    private readonly IReadOnlyList<ShellInfo> _availableShells;
    private readonly ISettingsProvider _settingsProvider;
    private readonly SillViewBase _sillView;

    internal OnGoingCommandsPopup(
        CommandExecutionService commandExecutionService,
        IReadOnlyList<ShellInfo> availableShells,
        ISettingsProvider settingsProvider,
        OnGoingCommandsViewModel viewModel,
        SillViewBase sillView)
    {
        _commandExecutionService = commandExecutionService;
        _availableShells = availableShells;
        _settingsProvider = settingsProvider;
        _sillView = sillView;
        ViewModel = viewModel;
        InitializeComponent();
    }

    internal OnGoingCommandsViewModel ViewModel { get; }

    private void ShortcutListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is CommandRunnerHandle commandRunner)
        {
            ShortcutListView_ItemInvoked(sender, commandRunner);
        }
    }

    private void ShortcutListView_ItemInvoked(object sender, CommandRunnerHandle e)
    {
        var sillPopup = new SillPopup();
        sillPopup.Content = new CommandPopup(new CommandViewModel(_commandExecutionService, e, _availableShells, _settingsProvider));

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

    private void DismissButton_Click(object sender, RoutedEventArgs e)
    {
        var commandRunnerHandler = (CommandRunnerHandle)((FrameworkElement)sender).DataContext;
        _commandExecutionService.Destroy(commandRunnerHandler.Id);
    }

    private void DismissAllButton_Click(object sender, RoutedEventArgs e)
    {
        _commandExecutionService.DestroyAllStartedRunners();
        Close();
    }
}
