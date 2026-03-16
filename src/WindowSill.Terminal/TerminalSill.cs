using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.UI.Xaml.Media.Imaging;
using WindowSill.API;
using WindowSill.Terminal.Core;
using WindowSill.Terminal.ViewModels;
using WindowSill.Terminal.Views;
using Path = System.IO.Path;

namespace WindowSill.Terminal;

/// <summary>
/// WindowSill Terminal extension entry point. Detects command-like text selections
/// and offers to execute them in a user-chosen shell.
/// </summary>
[Export(typeof(ISill))]
[Name("Terminal")]
public sealed class TerminalSill : ISillActivatedByTextSelection, ISillActivatedByDefault, ISillListView
{
    private readonly List<CommandItemViewModel> _activeCommands = [];

    [Import]
    private IPluginInfo _pluginInfo = null!;

    [Import]
    private IShellDetectionService _shellDetectionService = null!;

    [Import]
    private ICommandExecutionService _commandExecutionService = null!;

    /// <inheritdoc />
    public string DisplayName => "/WindowSill.Terminal/TerminalSill/DisplayName".GetLocalizedString();

    /// <inheritdoc />
    public SillSettingsView[]? SettingsViews => null;

    /// <inheritdoc />
    public ObservableCollection<SillListViewItem> ViewList { get; } = [];

    /// <inheritdoc />
    public SillView? PlaceholderView => null;

    /// <inheritdoc />
    public string[] TextSelectionActivatorTypeNames => [Activators.CommandSelectionActivator.ActivatorName];

    /// <inheritdoc />
    public IconElement CreateIcon()
        => new ImageIcon
        {
            Source = new SvgImageSource(new Uri(Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "terminal.svg")))
        };

    /// <inheritdoc />
    public async ValueTask OnActivatedAsync()
    {
        // Default activation — rebuild view list to show any active commands.
        await ThreadHelper.RunOnUIThreadAsync(RebuildViewList);
    }

    /// <inheritdoc />
    public async ValueTask OnActivatedAsync(string textSelectionActivatorTypeName, WindowTextSelection currentSelection)
    {
        IReadOnlyList<ShellInfo> shells = _shellDetectionService.GetAvailableShells();
        if (shells.Count == 0)
        {
            return;
        }

        var viewModel = new CommandItemViewModel(
            currentSelection.SelectedText.Trim(),
            shells,
            _commandExecutionService);

        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            var popup = new TerminalPopup(viewModel);

            var listItem = new SillListViewPopupItem(
                currentSelection.SelectedText.Trim(),
                null,
                popup);

            viewModel.DismissRequested += (_, _) =>
            {
                ThreadHelper.RunOnUIThreadAsync(() =>
                {
                    ViewList.Remove(listItem);
                    _activeCommands.Remove(viewModel);
                }).ForgetSafely();
            };

            _activeCommands.Add(viewModel);
            ViewList.Add(listItem);
        });
    }

    /// <inheritdoc />
    public async ValueTask OnDeactivatedAsync()
    {
        // Only remove pending (not yet running) commands.
        // Running/completed commands remain in the list view.
        await ThreadHelper.RunOnUIThreadAsync(RebuildViewList);
    }

    private void RebuildViewList()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        ViewList.Clear();

        // Keep items that are running or finished (not pending).
        for (int i = _activeCommands.Count - 1; i >= 0; i--)
        {
            CommandItemViewModel vm = _activeCommands[i];
            if (vm.State == CommandState.Pending)
            {
                _activeCommands.RemoveAt(i);
                continue;
            }

            var popup = new TerminalPopup(vm);
            var listItem = new SillListViewPopupItem(
                vm.CommandText,
                null,
                popup);

            vm.DismissRequested += (_, _) =>
            {
                ThreadHelper.RunOnUIThreadAsync(() =>
                {
                    ViewList.Remove(listItem);
                    _activeCommands.Remove(vm);
                }).ForgetSafely();
            };

            ViewList.Add(listItem);
        }
    }
}
