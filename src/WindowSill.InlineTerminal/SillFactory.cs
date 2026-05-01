using System.ComponentModel.Composition;
using WindowSill.API;
using WindowSill.InlineTerminal.Core.Parsers;
using WindowSill.InlineTerminal.Core.Shell;
using WindowSill.InlineTerminal.Models;
using WindowSill.InlineTerminal.Services;
using WindowSill.InlineTerminal.ViewModels;
using WindowSill.InlineTerminal.Views;
using Path = System.IO.Path;

namespace WindowSill.InlineTerminal;

/// <summary>
/// Creates sill view items for commands. Uses <see cref="CommandService"/> for command lifecycle
/// and <see cref="MenuFlyoutBuilder"/> for dynamic context menus.
/// </summary>
[Export]
internal sealed class SillFactory
{
    private readonly HashSet<string> _cmdFileExtensions
        = new(StringComparer.OrdinalIgnoreCase) { ".bat", ".cmd", ".com" };

    private readonly HashSet<string> _powerShellFileExtensions
        = new(StringComparer.OrdinalIgnoreCase) { ".ps1" };

    private readonly HashSet<string> _wslFileExtensions
        = new(StringComparer.OrdinalIgnoreCase) { ".sh", ".bash", ".zsh" };

    private readonly ShellDetectionService _shellDetectionService;
    private readonly CommandService _commandService;
    private readonly ISettingsProvider _settingsProvider;

    [ImportingConstructor]
    public SillFactory(
        ShellDetectionService shellDetectionService,
        CommandService commandService,
        ISettingsProvider settingsProvider)
    {
        _shellDetectionService = shellDetectionService;
        _commandService = commandService;
        _settingsProvider = settingsProvider;
    }

    internal async Task<SillListViewPopupItem?> CreateOnGoingCommandsPopupAsync()
    {
        IReadOnlyList<ShellInfo> availableShells = await _shellDetectionService.GetAvailableShellsAsync();
        if (availableShells.Count == 0)
        {
            return null;
        }

        var viewModel = new OnGoingCommandsViewModel(_commandService);

        var sillView
            = new SillListViewPopupItem(
                new OnGoingCommandsSillContent(viewModel),
                null,
                null!);

        sillView.PopupContent = new OnGoingCommandsPopup(_commandService, availableShells, _settingsProvider, viewModel, sillView);

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

            CommandDefinition command = _commandService.CreateCommand(
                script: null,
                scriptFilePath,
                workingDirectory,
                defaultShell,
                source: null);

            var viewModel = new CommandViewModel(_commandService, command, availableShells, _settingsProvider);
            var popup = new CommandPopup(viewModel);

            var menuFlyout = new MenuFlyout();
            menuFlyout.Opening += (_, _) =>
                MenuFlyoutBuilder.PopulateMenu(menuFlyout, command, viewModel, hasSelectedText: false, _commandService);

            var sillView
                = new SillListViewPopupItem(
                    new CommandSillContent(viewModel),
                    scriptFilePath,
                    popup)
                {
                    ContextFlyout = menuFlyout
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

            ShellInfo? defaultShell = DetectShell(windowTextSelection.SelectedText, availableShells);

            foreach (ParsedCommandBlock block in blocks)
            {
                CommandDefinition command = _commandService.CreateCommand(
                    block.Command,
                    scriptFilePath: null,
                    block.WorkingDirectory,
                    defaultShell,
                    windowTextSelection);

                var viewModel = new CommandViewModel(_commandService, command, availableShells, _settingsProvider);
                var popup = new CommandPopup(viewModel);

                var menuFlyout = new MenuFlyout();
                menuFlyout.Opening += (_, _) =>
                    MenuFlyoutBuilder.PopulateMenu(menuFlyout, command, viewModel, hasSelectedText: true, _commandService);

                var sillView
                    = new SillListViewPopupItem(
                        new CommandSillContent(viewModel),
                        new CommandSillPreview(viewModel),
                        popup)
                    {
                        ContextFlyout = menuFlyout
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
    /// Creates sill view items from pre-computed shell and command data.
    /// Must be called on the UI thread since it creates XAML objects.
    /// </summary>
    internal List<SillListViewPopupItem> CreateSillsFromPrecomputedData(
        WindowTextSelection windowTextSelection,
        IReadOnlyList<ShellInfo> availableShells,
        List<ParsedCommandBlock> blocks,
        ShellInfo defaultShell)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        var results = new List<SillListViewPopupItem>();

        try
        {
            foreach (ParsedCommandBlock block in blocks)
            {
                CommandDefinition command = _commandService.CreateCommand(
                    block.Command,
                    scriptFilePath: null,
                    block.WorkingDirectory,
                    defaultShell,
                    windowTextSelection);

                var viewModel = new CommandViewModel(_commandService, command, availableShells, _settingsProvider);
                var popup = new CommandPopup(viewModel);

                var menuFlyout = new MenuFlyout();
                menuFlyout.Opening += (_, _) =>
                    MenuFlyoutBuilder.PopulateMenu(menuFlyout, command, viewModel, hasSelectedText: true, _commandService);

                var sillView
                    = new SillListViewPopupItem(
                        new CommandSillContent(viewModel),
                        new CommandSillPreview(viewModel),
                        popup)
                    {
                        ContextFlyout = menuFlyout
                    };

                results.Add(sillView);
            }
        }
        catch
        {
        }

        return results;
    }

    internal static ShellInfo DetectShell(string selectedText, IReadOnlyList<ShellInfo> shells)
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
}
