using CommunityToolkit.Mvvm.ComponentModel;
using WindowSill.InlineTerminal.Models;

namespace WindowSill.InlineTerminal.ViewModels;

/// <summary>
/// Lightweight view model for displaying a single command run in the on-going commands list.
/// Provides bindable properties for XAML data templates.
/// </summary>
public sealed class ActiveRunItem : ObservableObject
{
    internal ActiveRunItem(CommandDefinition command, CommandRun run)
    {
        Command = command;
        Run = run;
    }

    /// <summary>
    /// Gets the parent command definition.
    /// </summary>
    internal CommandDefinition Command { get; }

    /// <summary>
    /// Gets the run instance.
    /// </summary>
    internal CommandRun Run { get; }

    /// <summary>
    /// Gets the command's display title.
    /// </summary>
    public string Title => Command.Title;

    /// <summary>
    /// Gets the first line of output, trimmed, for display previews.
    /// </summary>
    public string OutputTrimmed => Run.OutputTrimmed;

    /// <summary>
    /// Gets the current execution state of this run.
    /// </summary>
    public CommandState State => Run.State;

    /// <summary>
    /// Gets the run's start time formatted for display.
    /// </summary>
    public string StartedAt => Run.StartedAt.ToLocalTime().ToString("T");
}
