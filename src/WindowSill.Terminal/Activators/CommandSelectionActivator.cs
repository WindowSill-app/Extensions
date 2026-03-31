using System.ComponentModel.Composition;
using WindowSill.API;
using WindowSill.Terminal.Parsers;

namespace WindowSill.Terminal.Activators;

/// <summary>
/// Activates when the selected text appears to be a recognized command
/// found on the system PATH.
/// </summary>
[Export(typeof(ISillTextSelectionActivator))]
[ActivationType(ActivatorName, PredefinedActivationTypeNames.TextSelection)]
internal sealed class CommandSelectionActivator : ISillTextSelectionActivator
{
    internal const string ActivatorName = "CommandSelection";

    /// <summary>
    /// Returns <see langword="true"/> when the first token of
    /// <paramref name="selectedText"/> matches an executable on the system PATH.
    /// </summary>
    public ValueTask<bool> GetShouldBeActivatedAsync(
        string selectedText,
        bool isReadOnly,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(selectedText))
        {
            return new ValueTask<bool>(false);
        }

        try
        {
            string? firstCommandWithoutDecorators = TerminalCommandParser.GetFirstTerminalCommand(selectedText);
            if (firstCommandWithoutDecorators is null)
            {
                return new ValueTask<bool>(false);
            }

            return new ValueTask<bool>(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // IO or other unexpected errors — not a command.
        }

        return new ValueTask<bool>(false);
    }
}
