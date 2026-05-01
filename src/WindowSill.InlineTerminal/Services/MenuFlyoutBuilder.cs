using WindowSill.API;
using WindowSill.InlineTerminal.Models;
using WindowSill.InlineTerminal.ViewModels;

namespace WindowSill.InlineTerminal.Services;

/// <summary>
/// Builds context menu flyout items from current command state.
/// Rebuilds on every <see cref="MenuFlyout.Opening"/> to ensure freshness.
/// </summary>
internal static class MenuFlyoutBuilder
{
    private const int HorizontalCharacterLimit = 50;

    /// <summary>
    /// Builds a context menu flyout for a command definition.
    /// Called on each <see cref="MenuFlyout.Opening"/> event to reflect current state.
    /// </summary>
    internal static void PopulateMenu(
        MenuFlyout menuFlyout,
        CommandDefinition command,
        CommandViewModel viewModel,
        bool hasSelectedText,
        CommandService commandService)
    {
        menuFlyout.Items.Clear();

        // Run count
        if (command.HasBeenExecuted)
        {
            menuFlyout.Items.Add(new MenuFlyoutItem
            {
                IsEnabled = false,
                Text = string.Format(
                    "/WindowSill.InlineTerminal/TerminalSill/RunCount".GetLocalizedString(),
                    command.Runs.Count),
                Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE9D5" },
            });

            menuFlyout.Items.Add(new MenuFlyoutSeparator());
        }

        // Cancel (only when the latest run is in progress)
        if (command.LatestRun is { State: CommandState.Running } runningRun)
        {
            var cancelItem = new MenuFlyoutItem
            {
                Text = "/WindowSill.InlineTerminal/TerminalSill/CancelRun".GetLocalizedString(),
                Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE711" },
            };
            cancelItem.Click += (_, _) => commandService.CancelRun(runningRun.Id);
            menuFlyout.Items.Add(cancelItem);
            menuFlyout.Items.Add(new MenuFlyoutSeparator());
        }

        // Dismiss actions (only shown for executed commands)
        if (command.HasBeenExecuted)
        {
            bool menuAdded = false;

            CommandRun? latestRun = command.LatestRun;

            // "Dismiss" — dismisses the latest run.
            if (latestRun is not null)
            {
                var dismissThisItem = new MenuFlyoutItem
                {
                    Text = "/WindowSill.InlineTerminal/TerminalSill/DismissThisRun".GetLocalizedString(),
                    Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE74D" },
                };
                dismissThisItem.Click += (_, _) => commandService.DismissRun(command.Id, latestRun.Id);
                menuFlyout.Items.Add(dismissThisItem);
                menuAdded = true;
            }

            int otherCount = command.Runs.Count - 1;

            // "Dismiss N other run(s)" — dismisses all runs except the latest.
            if (latestRun is not null && otherCount > 0)
            {
                var dismissOthersItem = new MenuFlyoutItem
                {
                    Text = string.Format(
                        "/WindowSill.InlineTerminal/TerminalSill/DismissOtherRuns".GetLocalizedString(),
                        otherCount),
                    Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE74D" },
                };
                dismissOthersItem.Click += (_, _) =>
                    commandService.DismissOtherRuns(command.Id, latestRun.Id);
                menuFlyout.Items.Add(dismissOthersItem);
                menuAdded = true;
            }

            // "Dismiss all N runs" — dismisses everything.
            if (command.Runs.Count > 1)
            {
                var dismissAllItem = new MenuFlyoutItem
                {
                    Text = string.Format(
                        "/WindowSill.InlineTerminal/TerminalSill/DismissAllRuns".GetLocalizedString(),
                        command.Runs.Count),
                    Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE74D" },
                };
                dismissAllItem.Click += (_, _) =>
                    commandService.DismissAllRuns(command.Id);
                menuFlyout.Items.Add(dismissAllItem);
                menuAdded = true;
            }

            if (menuAdded)
            {
                menuFlyout.Items.Add(new MenuFlyoutSeparator());
            }
        }

        // Detected run settings
        menuFlyout.Items.Add(new MenuFlyoutItem
        {
            IsEnabled = false,
            Text = "/WindowSill.InlineTerminal/TerminalSill/DetectedRunSettings".GetLocalizedString(),
        });

        if (!string.IsNullOrEmpty(command.Title))
        {
            string displayTitle = command.Title.Length > HorizontalCharacterLimit
                ? $"{command.Title[..HorizontalCharacterLimit]}…"
                : command.Title;

            var commandTextItem = new MenuFlyoutItem
            {
                IsEnabled = false,
                Text = displayTitle,
                Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE756" },
            };
            ToolTipService.SetToolTip(commandTextItem, viewModel.Script);
            menuFlyout.Items.Add(commandTextItem);
        }

        string wd = viewModel.WorkingDirectory;
        string displayWd = wd.Length > HorizontalCharacterLimit
            ? $"{wd[..HorizontalCharacterLimit]}…"
            : wd;

        var wdItem = new MenuFlyoutItem
        {
            IsEnabled = false,
            Text = displayWd,
            Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE62F" },
        };
        ToolTipService.SetToolTip(wdItem, wd);
        menuFlyout.Items.Add(wdItem);

        if (viewModel.SelectedShell is not null)
        {
            var shellItem = new MenuFlyoutItem
            {
                IsEnabled = false,
                Text = viewModel.SelectedShell.DisplayName,
                Icon = viewModel.SelectedShell.Icon.Result is { } shellIcon
                    ? new ImageIcon { Source = shellIcon, Width = 16, Height = 16 }
                    : new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uECAA" },
            };
            ToolTipService.SetToolTip(shellItem, viewModel.SelectedShell.ExecutablePath);
            menuFlyout.Items.Add(shellItem);
        }

        menuFlyout.Items.Add(new MenuFlyoutSeparator());

        // Run / Re-run
        string runLabel = command.HasBeenExecuted
            ? "/WindowSill.InlineTerminal/TerminalSill/RerunNow".GetLocalizedString()
            : "/WindowSill.InlineTerminal/TerminalSill/RunNow".GetLocalizedString();

        menuFlyout.Items.Add(new MenuFlyoutItem
        {
            Text = runLabel,
            Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE76C" },
            Command = viewModel.RunMenuCommand,
        });

        if (hasSelectedText)
        {
            string runAndLabel = command.HasBeenExecuted
                ? "/WindowSill.InlineTerminal/TerminalSill/RerunAnd".GetLocalizedString()
                : "/WindowSill.InlineTerminal/TerminalSill/RunAnd".GetLocalizedString();

            var runAndSubMenu = new MenuFlyoutSubItem
            {
                Text = runAndLabel,
                Icon = new FontIcon { FontFamily = new FontFamily("Segoe Fluent Icons"), Glyph = "\uE76C" }
            };

            runAndSubMenu.Items.Add(new MenuFlyoutItem
            {
                Text = "/WindowSill.InlineTerminal/TerminalSill/CopyOutput".GetLocalizedString(),
                Icon = new SymbolIcon(Symbol.Copy),
                Command = viewModel.RunAndCopyMenuCommand,
            });

            runAndSubMenu.Items.Add(new MenuFlyoutItem
            {
                Text = "/WindowSill.InlineTerminal/TerminalSill/AppendSelection".GetLocalizedString(),
                Icon = new SymbolIcon(Symbol.Import),
                Command = viewModel.RunAndAppendMenuCommand,
            });

            runAndSubMenu.Items.Add(new MenuFlyoutItem
            {
                Text = "/WindowSill.InlineTerminal/TerminalSill/ReplaceSelection".GetLocalizedString(),
                Icon = new SymbolIcon(Symbol.ImportAll),
                Command = viewModel.RunAndReplaceMenuCommand,
            });

            menuFlyout.Items.Add(runAndSubMenu);
        }
        else
        {
            string runAndCopyLabel = command.HasBeenExecuted
                ? "/WindowSill.InlineTerminal/TerminalSill/RerunAndCopyOutput".GetLocalizedString()
                : "/WindowSill.InlineTerminal/TerminalSill/RunAndCopyOutput".GetLocalizedString();

            menuFlyout.Items.Add(new MenuFlyoutItem
            {
                Text = runAndCopyLabel,
                Icon = new SymbolIcon(Symbol.Copy),
                Command = viewModel.RunAndCopyMenuCommand,
            });
        }

        // Copy latest output (only if there's output)
        if (command.LatestRun is { } lastRun && !string.IsNullOrEmpty(lastRun.Output))
        {
            menuFlyout.Items.Add(new MenuFlyoutItem
            {
                Text = "/WindowSill.InlineTerminal/TerminalSill/CopyLatestOutput".GetLocalizedString(),
                Icon = new SymbolIcon(Symbol.Copy),
                Command = viewModel.CopyLatestOutputCommand,
            });
        }
    }
}
