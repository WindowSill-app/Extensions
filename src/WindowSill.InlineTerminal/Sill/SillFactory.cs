using System.ComponentModel.Composition;
using CommunityToolkit.Mvvm.Messaging;
using WindowSill.API;
using WindowSill.InlineTerminal.Core.Commands;
using WindowSill.InlineTerminal.Core.Parsers;
using WindowSill.InlineTerminal.Core.Shell;
using WindowSill.InlineTerminal.ViewModels;
using WindowSill.InlineTerminal.Views;
using Path = System.IO.Path;

namespace WindowSill.InlineTerminal.Sill;

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

    private readonly IMessenger _messenger;
    private readonly IPluginInfo _pluginInfo;
    private readonly ShellDetectionService _shellDetectionService;
    private readonly CommandExecutionService _commandExecutionService;

    [ImportingConstructor]
    public SillFactory(
        IMessenger messenger,
        IPluginInfo pluginInfo,
        ShellDetectionService shellDetectionService,
        CommandExecutionService commandExecutionService)
    {
        _messenger = messenger;
        _pluginInfo = pluginInfo;
        _shellDetectionService = shellDetectionService;
        _commandExecutionService = commandExecutionService;
    }

    internal async Task<SillListViewMenuFlyoutItem?> CreateSillFromScriptFilePathAsync(string scriptFilePath)
    {
        try
        {
            IReadOnlyList<ShellInfo> shells = await _shellDetectionService.GetAvailableShellsAsync();
            if (shells.Count == 0)
            {
                return null;
            }

            if (!File.Exists(scriptFilePath))
            {
                return null;
            }

            string? workingDirectory = Path.GetDirectoryName(scriptFilePath);

            string fileExtension = Path.GetExtension(scriptFilePath);

            ShellInfo? preferredTerminalShell = null;
            if (_cmdFileExtensions.Contains(fileExtension))
            {
                preferredTerminalShell = shells.FirstOrDefault(x => x.ExecutablePath.Contains("cmd.exe"));
            }
            else if (_powerShellFileExtensions.Contains(fileExtension))
            {
                preferredTerminalShell = shells.FirstOrDefault(x => x.ExecutablePath.Contains("powershell.exe"));
            }
            else if (_wslFileExtensions.Contains(fileExtension))
            {
                preferredTerminalShell = shells.FirstOrDefault(x => x.IsWsl);
            }

            var viewModel
                = new CommandViewModel(
                    _messenger,
                    _commandExecutionService,
                    windowTextSelection: null,
                    shells,
                    preferredTerminalShell,
                    workingDirectory,
                    script: null,
                    scriptFilePath);

            var sillView
                = new SillListViewMenuFlyoutItem(
                    string.Empty,
                    scriptFilePath,
                    CreateMenu(viewModel, hasSelectedText: false));

            sillView.Content = new CommandSillContent(_pluginInfo, sillView, viewModel);
            viewModel.SillView = sillView;

            return sillView;
        }
        catch
        {
        }

        return null;
    }

    internal async Task<SillListViewMenuFlyoutItem?> CreateSillFromSelectedTextAsync(WindowTextSelection windowTextSelection)
    {
        try
        {
            IReadOnlyList<ShellInfo> shells = await _shellDetectionService.GetAvailableShellsAsync();
            if (shells.Count == 0)
            {
                return null;
            }

            string? script = TerminalCommandParser.GetFirstTerminalCommand(windowTextSelection.SelectedText);
            if (script is null)
            {
                return null;
            }

            string? workingDirectory = TerminalCommandParser.GetFirstWorkingDirectory(windowTextSelection.SelectedText);

            ShellInfo? preferredTerminalShell = null;
            if (ShellHintDetector.HasPowerShellHint(windowTextSelection.SelectedText))
            {
                preferredTerminalShell = shells.FirstOrDefault(x => x.ExecutablePath.Contains("powershell.exe"));
            }
            else if (ShellHintDetector.HasPwshHint(windowTextSelection.SelectedText))
            {
                preferredTerminalShell = shells.FirstOrDefault(x => x.ExecutablePath.Contains("pwsh.exe"));
            }
            else if (ShellHintDetector.HasCmdHint(windowTextSelection.SelectedText))
            {
                preferredTerminalShell = shells.FirstOrDefault(x => x.ExecutablePath.Contains("cmd.exe"));
            }
            else if (ShellHintDetector.HasWslHint(windowTextSelection.SelectedText))
            {
                preferredTerminalShell = shells.FirstOrDefault(x => x.IsWsl);
            }

            var viewModel
                = new CommandViewModel(
                    _messenger,
                    _commandExecutionService,
                    windowTextSelection,
                    shells,
                    preferredTerminalShell,
                    workingDirectory,
                    script,
                    scriptFilePath: null);

            var sillView
                = new SillListViewMenuFlyoutItem(
                    string.Empty,
                    script,
                    CreateMenu(viewModel, hasSelectedText: true));

            sillView.Content = new CommandSillContent(_pluginInfo, sillView, viewModel);
            viewModel.SillView = sillView;

            return sillView;
        }
        catch
        {
        }

        return null;
    }

    internal SillListViewMenuFlyoutItem CreateSillFromCommandRunner(CommandRunner commandRunner)
    {
        var viewModel
            = new CommandViewModel(
                    _messenger,
                    _commandExecutionService,
                    commandRunner);

        var sillView
            = new SillListViewMenuFlyoutItem(
                string.Empty,
                commandRunner.ScriptFilePath ?? commandRunner.Script,
                CreateMenu(viewModel, hasSelectedText: false));

        sillView.Content = new CommandSillContent(_pluginInfo, sillView, viewModel);
        viewModel.SillView = sillView;

        return sillView;
    }

    private static MenuFlyout CreateMenu(CommandViewModel viewModel, bool hasSelectedText)
    {
        int horizontalCharacterLimit = 100;
        bool alreadyRan = viewModel.State is CommandState.Completed or CommandState.Failed or CommandState.Cancelled;
        string runOrRerunDisplayText = alreadyRan ? "Rerun" : "Run";

        var menuFlyout = new MenuFlyout();

        menuFlyout.Items.Add(new MenuFlyoutItem
        {
            IsEnabled = false,
            Text = $"Detected run settings:",
        });

        if (!string.IsNullOrEmpty(viewModel.Script))
        {
            var commandTextFlyoutItem = new MenuFlyoutItem
            {
                IsEnabled = false,
                Text = $"{new string(viewModel.Script.Take(horizontalCharacterLimit).ToArray())}{(viewModel.Script.Length > horizontalCharacterLimit ? "…" : string.Empty)}",
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
            Text = "Configure run",
            Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uF8A6" },
            Command = viewModel.ConfigureRunCommand
        });

        menuFlyout.Items.Add(new MenuFlyoutItem
        {
            Text = $"{runOrRerunDisplayText} now",
            Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE76C" },
            Command = viewModel.RunMenuCommand,
        });

        if (hasSelectedText)
        {
            var runAndMenuItem = new MenuFlyoutSubItem
            {
                Text = $"{runOrRerunDisplayText} and...",
                Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE76C" }
            };

            runAndMenuItem.Items.Add(new MenuFlyoutItem
            {
                Text = $"Copy output",
                Icon = new SymbolIcon(Symbol.Copy),
                Command = viewModel.RunAndCopyMenuCommand,
            });

            runAndMenuItem.Items.Add(new MenuFlyoutItem
            {
                Text = $"Append selection",
                Icon = new SymbolIcon(Symbol.Import),
                Command = viewModel.RunAndAppendMenuCommand,
            });

            runAndMenuItem.Items.Add(new MenuFlyoutItem
            {
                Text = $"Replace selection",
                Icon = new SymbolIcon(Symbol.ImportAll),
                Command = viewModel.RunAndReplaceMenuCommand,
            });

            menuFlyout.Items.Add(runAndMenuItem);
        }
        else
        {
            menuFlyout.Items.Add(new MenuFlyoutItem
            {
                Text = $"{runOrRerunDisplayText} and copy output",
                Icon = new SymbolIcon(Symbol.Copy),
                Command = viewModel.RunAndCopyMenuCommand,
            });
        }

        return menuFlyout;
    }
}
