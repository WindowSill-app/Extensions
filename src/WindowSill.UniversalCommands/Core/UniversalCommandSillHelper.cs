using Microsoft.UI.Xaml.Media.Imaging;
using WindowSill.API;

namespace WindowSill.UniversalCommands.Core;

internal sealed class UniversalCommandSillHelper
{
    /// <summary>
    /// Creates a <see cref="SillListViewButtonItem"/> for a global command
    /// that sends keys to the last active window.
    /// </summary>
    internal static SillListViewButtonItem CreateButtonItem(
        UniversalCommand command,
        IProcessInteractionService processInteractionService)
    {
        Func<Task> handler = () => ExecuteActionAsync(command, processInteractionService, windowInfo: null);
        return CreateButtonItemCore(command, handler);
    }

    /// <summary>
    /// Creates a <see cref="SillListViewButtonItem"/> for a process-specific command
    /// that sends keys to a specific window.
    /// </summary>
    internal static SillListViewButtonItem CreateButtonItem(
        UniversalCommand command,
        IProcessInteractionService processInteractionService,
        WindowInfo windowInfo)
    {
        Func<Task> handler = () => ExecuteActionAsync(command, processInteractionService, windowInfo);
        return CreateButtonItemCore(command, handler);
    }

    private static SillListViewButtonItem CreateButtonItemCore(UniversalCommand command, Func<Task> handler)
    {
        if (!string.IsNullOrEmpty(command.IconImagePath))
        {
            string path = command.IconImagePath;
            ImageSource source = path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
                ? new SvgImageSource(new Uri(path))
                : new BitmapImage(new Uri(path));

            return new SillListViewButtonItem(source, command.Name, handler);
        }

        return new SillListViewButtonItem(command.IconGlyph, command.Name, handler);
    }

    private static async Task ExecuteActionAsync(
        UniversalCommand command,
        IProcessInteractionService processInteractionService,
        WindowInfo? windowInfo)
    {
        switch (command.Type)
        {
            case UniversalCommandType.KeyboardShortcut:
                await KeyboardHelper.ExecuteChordAsync(command.KeyboardChordKeys, processInteractionService, windowInfo);
                break;

            case UniversalCommandType.PowerShellCommand:
                await PowerShellHelper.ExecuteAsync(command.PowerShellCommand);
                break;
        }
    }
}
