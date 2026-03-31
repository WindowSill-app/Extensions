using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using WindowSill.API;
using WindowSill.Terminal.Core;
using WindowSill.Terminal.Parsers;
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

    [Import]
    private IProcessInteractionService _processInteractionService = null!;

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
        // Available shells
        IReadOnlyList<ShellInfo> shells = _shellDetectionService.GetAvailableShells();
        if (shells.Count == 0)
        {
            return;
        }

        // Command input from selection
        string? terminalCommand = TerminalCommandParser.GetFirstTerminalCommand(currentSelection.SelectedText);
        if (terminalCommand is null)
        {
            return;
        }

        // Command data/display -- shown to user on sill, used to drive popup.
        var viewModel = new CommandItemViewModel(terminalCommand, currentSelection.SelectedText, shells, _commandExecutionService, _processInteractionService);

        // Working directory from selection
        string? workingDirectory = TerminalCommandParser.GetFirstWorkingDirectory(currentSelection.SelectedText);
        if (workingDirectory is not null)
        {
            viewModel.WorkingDirectory = workingDirectory;
        }

        // ShellInfo preference hints from selection
        ShellInfo? preferredTerminalShell = null;
        if (TerminalCommandParser.HasPowerShellHint(currentSelection.SelectedText))
        {
            preferredTerminalShell = shells.First(x => x.ExecutablePath.Contains("powershell.exe"));
        }
        else if (TerminalCommandParser.HasPwshHint(currentSelection.SelectedText))
        {
            preferredTerminalShell = shells.First(x => x.ExecutablePath.Contains("pwsh.exe"));
        }
        else if (TerminalCommandParser.HasCmdHint(currentSelection.SelectedText))
        {
            preferredTerminalShell = shells.First(x => x.ExecutablePath.Contains("cmd.exe"));
        }
        else if (TerminalCommandParser.HasWslHint(currentSelection.SelectedText) && false) // Disabled until implemented
        {
            throw new NotImplementedException(); // TODO
        }

        if (preferredTerminalShell is not null)
        {
            viewModel.SelectedShell = preferredTerminalShell;
        }

        // Add to sill ui as button with popup
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            var popup = new TerminalPopup(viewModel);

            var listItem = new SillListViewPopupItem(
                terminalCommand,
                null,
                popup);

            viewModel.DismissRequested += (_, _) =>
            {
                ThreadHelper.RunOnUIThreadAsync(() =>
                {
                    ViewList.Remove(listItem);
                    _activeCommands.Remove(viewModel);

                    ThreadHelper.RunOnUIThreadAsync(RebuildViewList);
                }).ForgetSafely();
            };

            viewModel.ExecutionStarted += (_, _) =>
            {
                ThreadHelper.RunOnUIThreadAsync(RebuildViewList);
            };

            viewModel.ExecutionCompleted += (_, _) =>
            {
                ThreadHelper.RunOnUIThreadAsync(RebuildViewList);
            };

            CreateContextMenu(viewModel, listItem);
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

            CreateContextMenu(vm, listItem);
            ViewList.Add(listItem);
        }
    }

    private void CreateContextMenu(CommandItemViewModel viewModel, SillListViewItem view)
    {
        int horizontalCharacterLimit = 25;
        bool alreadyRan = viewModel.State is CommandState.Completed or CommandState.Failed or CommandState.Cancelled;
        bool currentlyRunning = viewModel.State is CommandState.Running or CommandState.LaunchedElevated;
        string runOrRerunDisplayText = alreadyRan ? "Rerun" : "Run";

        var menuFlyout = new MenuFlyout();

        menuFlyout.Items.Add(new MenuFlyoutItem
        {
            IsEnabled = false,
            Text = $"Detected run settings:",
        });

        var commandTextFlyoutItem = new MenuFlyoutItem
        {
            Opacity = 0.33,
            Text = $"{new string(viewModel.CommandText.Take(horizontalCharacterLimit).ToArray())}{(viewModel.CommandText.Length > horizontalCharacterLimit ? "..." : string.Empty)}",
            Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE950" },
        };

        menuFlyout.Items.Add(commandTextFlyoutItem);
        ToolTipService.SetToolTip(commandTextFlyoutItem, viewModel.CommandText);

        var workingDirectoryFlyoutItem = new MenuFlyoutItem
        {
            Opacity = 0.33,
            Text = $"{new string(viewModel.WorkingDirectory.Take(horizontalCharacterLimit).ToArray())}{(viewModel.WorkingDirectory.Length > horizontalCharacterLimit ? "..." : string.Empty)}",
            Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE62F" },
        };

        menuFlyout.Items.Add(workingDirectoryFlyoutItem);
        ToolTipService.SetToolTip(workingDirectoryFlyoutItem, viewModel.WorkingDirectory);

        var shellInfoFlyoutItem = new MenuFlyoutItem
        {
            Opacity = 0.33,
            Text = $"{viewModel.SelectedShell.DisplayName}",
            Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE756" }, // TODO adjust rendered image to match shell
        };

        menuFlyout.Items.Add(shellInfoFlyoutItem);
        ToolTipService.SetToolTip(shellInfoFlyoutItem, viewModel.SelectedShell.ExecutablePath);

        menuFlyout.Items.Add(new MenuFlyoutSeparator());

        /* TODO: Need to be able to open this TerminalPopup programatically.
        menuFlyout.Items.Add(new MenuFlyoutItem
        {
            Text = "Configure run",
            Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uF8A6" },
        });
        */

        menuFlyout.Items.Add(new MenuFlyoutItem
        {
            Text = $"{runOrRerunDisplayText} now",
            Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE76C" },
            Command = viewModel.RunCommand,
        });
        menuFlyout.Items.Add(new MenuFlyoutItem
        {
            Text = $"{runOrRerunDisplayText} now + copy output",
            Icon = new SymbolIcon(Symbol.Copy),
            Command = viewModel.RunAndCopyCommand,
        });
        menuFlyout.Items.Add(new MenuFlyoutItem
        {
            Text = $"{runOrRerunDisplayText} now + append selection",
            Icon = new SymbolIcon(Symbol.Import),
            Command = viewModel.RunAndCopyAndPasteAppendedCommand,
        });
        menuFlyout.Items.Add(new MenuFlyoutItem
        {
            Text = $"{runOrRerunDisplayText} now + replace selection",
            Icon = new SymbolIcon(Symbol.ImportAll),
            Command = viewModel.RunAndCopyAndPasteReplaceCommand,
        });

        bool afterCopySeparatedAdded = false;

        if (alreadyRan)
        {
            menuFlyout.Items.Add(new MenuFlyoutItem
            {
                Text = "Copy output",
                Icon = new SymbolIcon(Symbol.Copy),
                Command = viewModel.CopyOutputCommand,
            });

            menuFlyout.Items.Add(new MenuFlyoutSeparator());
            afterCopySeparatedAdded = true;
        }

        if (currentlyRunning)
        {
            if (!afterCopySeparatedAdded)
            {
                menuFlyout.Items.Add(new MenuFlyoutSeparator());
            }

            menuFlyout.Items.Add(new MenuFlyoutItem
            {
                Text = $"Cancel ({viewModel.State.ToString().ToLowerInvariant()})",
                Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE711" },
                Command = viewModel.CancelCommand,
            });
        }

        // If more than one can be dismissed, show "Dismiss all"
        var allSillItemsThatCanBeDismissed = _activeCommands.Where(x => x.State is CommandState.Completed or CommandState.Failed or CommandState.Cancelled).ToList();
        if (allSillItemsThatCanBeDismissed.Count > 0 && (allSillItemsThatCanBeDismissed.Count != 1 || !ReferenceEquals(allSillItemsThatCanBeDismissed.First(), viewModel)))
        {
            if (!afterCopySeparatedAdded)
            {
                menuFlyout.Items.Add(new MenuFlyoutSeparator());
            }

            int completedCount = allSillItemsThatCanBeDismissed.Count(x => x.State is CommandState.Completed);
            bool completedIncludesSelf = allSillItemsThatCanBeDismissed.Any(x => x.State is CommandState.Completed && ReferenceEquals(x, viewModel));

            int cancelledCount = allSillItemsThatCanBeDismissed.Count(x => x.State is CommandState.Cancelled);
            bool cancelledIncludesSelf = allSillItemsThatCanBeDismissed.Any(x => x.State is CommandState.Cancelled && ReferenceEquals(x, viewModel));

            int failedCount = allSillItemsThatCanBeDismissed.Count(x => x.State is CommandState.Failed);
            bool failedIncludesSelf = allSillItemsThatCanBeDismissed.Any(x => x.State is CommandState.Failed && ReferenceEquals(x, viewModel));

            menuFlyout.Items.Add(new MenuFlyoutItem
            {
                Text = $"Dismiss {(completedCount > 0 ? $"{completedCount}{(!completedIncludesSelf ? $" other" : string.Empty)} completed{(failedCount + cancelledCount > 0 ? ", " : string.Empty)}" : string.Empty)}{(failedCount > 0 ? $"{failedCount}{(!failedIncludesSelf ? $" other" : string.Empty)} failed{(cancelledCount > 0 ? ", " : string.Empty)}" : string.Empty)}{(cancelledCount > 0 ? $"{cancelledCount}{(!cancelledIncludesSelf ? $" other" : string.Empty)} cancelled" : string.Empty)} run{(allSillItemsThatCanBeDismissed.Count > 1 ? "s" : string.Empty)}",
                Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE711" },
                Command = new RelayCommand(() =>
                {
                    foreach (CommandItemViewModel item in allSillItemsThatCanBeDismissed)
                    {
                        if (item.DismissCommand.CanExecute(null))
                        {
                            item.DismissCommand.Execute(null);
                        }
                    }
                },
                canExecute: () =>
                {
                    return allSillItemsThatCanBeDismissed.Any(x => x.DismissCommand.CanExecute(null));
                }),
            });
        }

        if (alreadyRan)
        {
            menuFlyout.Items.Add(new MenuFlyoutItem
            {
                Text = $"Dismiss this run ({viewModel.State.ToString().ToLowerInvariant()})",
                Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE711" },
                Command = viewModel.DismissCommand,
            });
        }

        view.ContextFlyout = menuFlyout;
    }
}
