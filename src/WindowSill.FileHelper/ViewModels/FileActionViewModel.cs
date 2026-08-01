using System.Windows.Input;

namespace WindowSill.FileHelper.ViewModels;

/// <summary>
/// One action button in the popup: its label, a stable automation id, and the command that starts the work.
/// </summary>
/// <remarks>
/// The command is parameterless and already bound to everything it needs, which is what lets a single button
/// template serve both document conversion targets ("PDF", "Word", ...) and PDF actions ("Merge", "Split", ...).
/// It also has to live on the item itself, because a <c>DataTemplate</c> cannot reach back to the page's view model
/// with <c>x:Bind</c>.
/// </remarks>
internal sealed class FileActionViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileActionViewModel"/> class.
    /// </summary>
    /// <param name="displayName">The localized button label.</param>
    /// <param name="automationId">A stable, non-localized automation id for UI tests.</param>
    /// <param name="command">The command invoked when the button is pressed.</param>
    internal FileActionViewModel(string displayName, string automationId, ICommand command)
    {
        DisplayName = displayName;
        AutomationId = automationId;
        Command = command;
    }

    /// <summary>
    /// Gets the localized button label (e.g. "PDF", "Merge").
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets a stable, non-localized automation id (e.g. <c>ConvertToPdf</c>, <c>PdfActionMerge</c>) so UI tests can
    /// target a specific action regardless of display language.
    /// </summary>
    public string AutomationId { get; }

    /// <summary>
    /// Gets the command that starts this action.
    /// </summary>
    public ICommand Command { get; }
}
