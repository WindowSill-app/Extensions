using System.ComponentModel.Composition;
using WindowSill.API;
using Path = System.IO.Path;

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

    private static readonly string[] executableExtensions = [".exe", ".cmd", ".bat", ".ps1", ".com"];

    /// <summary>
    /// Returns <see langword="true"/> when the first token of
    /// <paramref name="selectedText"/> matches an executable on the system PATH.
    /// </summary>
    public ValueTask<bool> GetShouldBeActivatedAsync(
        string selectedText,
        bool isReadOnly,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(selectedText) || selectedText.Length > 500)
        {
            return new ValueTask<bool>(false);
        }

        string token = selectedText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0];

        try
        {
            string? pathValue = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathValue))
            {
                return new ValueTask<bool>(false);
            }

            string[] directories = pathValue.Split(Path.PathSeparator);

            foreach (string directory in directories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(directory))
                {
                    continue;
                }

                // Check the token as-is (may already include an extension).
                if (File.Exists(Path.Combine(directory, token)))
                {
                    return new ValueTask<bool>(true);
                }

                // Check with each known executable extension.
                foreach (string ext in executableExtensions)
                {
                    if (File.Exists(Path.Combine(directory, token + ext)))
                    {
                        return new ValueTask<bool>(true);
                    }
                }
            }
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
