using System.ComponentModel.Composition;
using WindowSill.API;
using WindowSill.InlineTerminal.Core.Commands;
using WindowSill.InlineTerminal.Core.Parsers;
using WindowSill.InlineTerminal.Core.Shell;
using WindowSill.InlineTerminal.ViewModels;
using WindowSill.InlineTerminal.Views;
using Path = System.IO.Path;

namespace WindowSill.InlineTerminal.Core;

[Export]
internal sealed class SillFactory
{
    private readonly HashSet<string> _cmdFileExtensions
        = new(StringComparer.OrdinalIgnoreCase)
        {
            ".bat",
            ".cmd",
            ".com"
        };

    private readonly HashSet<string> _powerShellFileExtensions
        = new(StringComparer.OrdinalIgnoreCase)
        {
            ".ps1"
        };

    private readonly HashSet<string> _wslFileExtensions
        = new(StringComparer.OrdinalIgnoreCase)
        {
            ".sh",
            ".bash",
            ".zsh"
        };

    private readonly ShellDetectionService _shellDetectionService;
    private readonly CommandExecutionService _commandExecutionService;
    private readonly ISettingsProvider _settingsProvider;

    [ImportingConstructor]
    public SillFactory(
        ShellDetectionService shellDetectionService,
        CommandExecutionService commandExecutionService,
        ISettingsProvider settingsProvider)
    {
        _shellDetectionService = shellDetectionService;
        _commandExecutionService = commandExecutionService;
        _settingsProvider = settingsProvider;
    }

    internal async Task<SillListViewPopupItem?> CreateOnGoingCommandsPopupAsync()
    {
        IReadOnlyList<ShellInfo> availableShells = await _shellDetectionService.GetAvailableShellsAsync();
        if (availableShells.Count == 0)
        {
            return null;
        }

        var viewModel = new OnGoingCommandsViewModel(_commandExecutionService);

        var sillView
            = new SillListViewPopupItem(
                new OnGoingCommandsSillContent(viewModel),
                null,
                null!);

        sillView.PopupContent = new OnGoingCommandsPopup(_commandExecutionService, availableShells, _settingsProvider, viewModel, sillView);

        return sillView;
    }

    internal async Task<SillListViewPopupItem?> CreateSillFromScriptFilePathAsync(string scriptFilePath)
    {
        try
        {
            IReadOnlyList<ShellInfo> availableShells = await _shellDetectionService.GetAvailableShellsAsync();
            if (availableShells.Count == 0)
            {
                return null;
            }

            if (!File.Exists(scriptFilePath))
            {
                return null;
            }

            string? workingDirectory = Path.GetDirectoryName(scriptFilePath);

            string fileExtension = Path.GetExtension(scriptFilePath);

            ShellInfo? defaultShell = null;
            if (_cmdFileExtensions.Contains(fileExtension))
            {
                defaultShell = availableShells.FirstOrDefault(x => x.ExecutablePath.Contains("cmd.exe"));
            }
            else if (_powerShellFileExtensions.Contains(fileExtension))
            {
                defaultShell = availableShells.FirstOrDefault(x => x.ExecutablePath.Contains("powershell.exe"));
            }
            else if (_wslFileExtensions.Contains(fileExtension))
            {
                defaultShell = availableShells.FirstOrDefault(x => x.IsWsl);
            }

            defaultShell ??= availableShells[0];

            CommandRunnerHandle commandRunnerHandle
                = await _commandExecutionService.CreateAsync(
                    windowTextSelection: null,
                    defaultShell,
                    workingDirectory,
                    script: null,
                    scriptFilePath);

            var viewModel = new CommandViewModel(_commandExecutionService, commandRunnerHandle, availableShells, _settingsProvider);
            var popup = new CommandPopup(viewModel);

            var sillView
                = new SillListViewPopupItem(
                    Path.GetFileName(scriptFilePath),
                    scriptFilePath,
                    popup)
                {
                    ContextFlyout = CreateMenu(commandRunnerHandle, viewModel, hasSelectedText: false)
                };

            return sillView;
        }
        catch
        {
        }

        return null;
    }

    internal async Task<List<SillListViewPopupItem>> CreateSillsFromSelectedTextAsync(WindowTextSelection windowTextSelection)
    {
        var results = new List<SillListViewPopupItem>();

        try
        {
            IReadOnlyList<ShellInfo> availableShells = await _shellDetectionService.GetAvailableShellsAsync();
            if (availableShells.Count == 0)
            {
                return results;
            }

            List<ParsedCommandBlock> blocks = TerminalCommandParser.GetCommandBlocks(windowTextSelection.SelectedText);
            if (blocks.Count == 0)
            {
                return results;
            }

            // TODO: Detect per command block.
            ShellInfo? defaultShell = DetectShell(windowTextSelection.SelectedText, availableShells);

            foreach (ParsedCommandBlock block in blocks)
            {
                CommandRunnerHandle commandRunnerHandle
                    = await _commandExecutionService.CreateAsync(
                        windowTextSelection,
                        defaultShell,
                        block.WorkingDirectory,
                        block.Command,
                        scriptFilePath: null);

                var viewModel = new CommandViewModel(_commandExecutionService, commandRunnerHandle, availableShells, _settingsProvider);
                var popup = new CommandPopup(viewModel);

                var sillView
                    = new SillListViewPopupItem(
                        commandRunnerHandle.Title,
                        block.Command,
                        popup)
                    {
                        ContextFlyout = CreateMenu(commandRunnerHandle, viewModel, hasSelectedText: true)
                    };

                results.Add(sillView);
            }
        }
        catch
        {
        }

        return results;
    }

    /// <summary>
    /// Detects which shell the user likely intends based on hints in the selected text.
    /// </summary>
    private static ShellInfo DetectShell(string selectedText, IReadOnlyList<ShellInfo> shells)
    {
        if (shells.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shells));
        }

        if (ShellHintDetector.HasPowerShellHint(selectedText))
        {
            return shells.First(x => x.ExecutablePath.Contains("powershell.exe", StringComparison.OrdinalIgnoreCase));
        }

        if (ShellHintDetector.HasPwshHint(selectedText))
        {
            return shells.First(x => x.ExecutablePath.Contains("pwsh.exe", StringComparison.OrdinalIgnoreCase));
        }

        if (ShellHintDetector.HasCmdHint(selectedText))
        {
            return shells.First(x => x.ExecutablePath.Contains("cmd.exe", StringComparison.OrdinalIgnoreCase));
        }

        if (ShellHintDetector.HasWslHint(selectedText))
        {
            return shells.First(x => x.IsWsl);
        }

        return shells[0];
    }

    private static MenuFlyout CreateMenu(CommandRunnerHandle commandRunnerHandle, CommandViewModel viewModel, bool hasSelectedText)
    {
        int horizontalCharacterLimit = 50;

        var menuFlyout = new MenuFlyout();

        menuFlyout.Items.Add(new MenuFlyoutItem
        {
            IsEnabled = false,
            Text = $"/WindowSill.InlineTerminal/TerminalSill/DetectedRunSettings".GetLocalizedString(),
        });

        if (!string.IsNullOrEmpty(commandRunnerHandle.Title))
        {
            var commandTextFlyoutItem = new MenuFlyoutItem
            {
                IsEnabled = false,
                Text = $"{new string(commandRunnerHandle.Title.Take(horizontalCharacterLimit).ToArray())}{(commandRunnerHandle.Title.Length > horizontalCharacterLimit ? "…" : string.Empty)}",
                Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE950" },
            };

            menuFlyout.Items.Add(commandTextFlyoutItem);
            ToolTipService.SetToolTip(commandTextFlyoutItem, viewModel.Script);
        }

        var workingDirectoryFlyoutItem = new MenuFlyoutItem
        {
            IsEnabled = false,
            Text = $"{new string(viewModel.WorkingDirectory.Take(horizontalCharacterLimit).ToArray())}{(viewModel.WorkingDirectory.Length > horizontalCharacterLimit ? "…" : string.Empty)}",
            Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE62F" },
        };

        menuFlyout.Items.Add(workingDirectoryFlyoutItem);
        ToolTipService.SetToolTip(workingDirectoryFlyoutItem, viewModel.WorkingDirectory);

        if (viewModel.SelectedShell is not null)
        {
            var shellInfoFlyoutItem = new MenuFlyoutItem
            {
                IsEnabled = false,
                Text = $"{viewModel.SelectedShell.DisplayName}",
                Icon = viewModel.SelectedShell.Icon is { } shellIcon
                    ? new ImageIcon { Source = shellIcon, Width = 16, Height = 16 }
                    : new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE756" },
            };

            menuFlyout.Items.Add(shellInfoFlyoutItem);
            ToolTipService.SetToolTip(shellInfoFlyoutItem, viewModel.SelectedShell.ExecutablePath);
        }

        menuFlyout.Items.Add(new MenuFlyoutSeparator());

        menuFlyout.Items.Add(new MenuFlyoutItem
        {
            Text = $"/WindowSill.InlineTerminal/TerminalSill/RunNow".GetLocalizedString(),
            Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE76C" },
            Command = viewModel.RunMenuCommand,
        });

        if (hasSelectedText)
        {
            var runAndMenuItem = new MenuFlyoutSubItem
            {
                Text = $"/WindowSill.InlineTerminal/TerminalSill/RunAnd".GetLocalizedString(),
                Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE76C" }
            };

            runAndMenuItem.Items.Add(new MenuFlyoutItem
            {
                Text = $"/WindowSill.InlineTerminal/TerminalSill/CopyOutput".GetLocalizedString(),
                Icon = new SymbolIcon(Symbol.Copy),
                Command = viewModel.RunAndCopyMenuCommand,
            });

            runAndMenuItem.Items.Add(new MenuFlyoutItem
            {
                Text = $"/WindowSill.InlineTerminal/TerminalSill/AppendSelection".GetLocalizedString(),
                Icon = new SymbolIcon(Symbol.Import),
                Command = viewModel.RunAndAppendMenuCommand,
            });

            runAndMenuItem.Items.Add(new MenuFlyoutItem
            {
                Text = $"/WindowSill.InlineTerminal/TerminalSill/ReplaceSelection".GetLocalizedString(),
                Icon = new SymbolIcon(Symbol.ImportAll),
                Command = viewModel.RunAndReplaceMenuCommand,
            });

            menuFlyout.Items.Add(runAndMenuItem);
        }
        else
        {
            menuFlyout.Items.Add(new MenuFlyoutItem
            {
                Text = $"/WindowSill.InlineTerminal/TerminalSill/RunAndCopyOutput".GetLocalizedString(),
                Icon = new SymbolIcon(Symbol.Copy),
                Command = viewModel.RunAndCopyMenuCommand,
            });
        }

        return menuFlyout;
    }
}
