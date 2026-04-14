using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using WindowSill.API;
using WindowSill.InlineTerminal.Models;

namespace WindowSill.InlineTerminal.Services;

/// <summary>
/// Abstracts clipboard operations and post-completion actions.
/// </summary>
internal static class ClipboardService
{
    /// <summary>
    /// Copies text to the clipboard.
    /// </summary>
    internal static async Task CopyAsync(string text, bool includeInClipboardHistory)
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            var dataPackage = new DataPackage();
            dataPackage.RequestedOperation = includeInClipboardHistory
                ? DataPackageOperation.Copy
                : DataPackageOperation.Move;

            dataPackage.SetText(text);

            Clipboard.SetContentWithOptions(
                dataPackage,
                new ClipboardContentOptions()
                {
                    IsAllowedInHistory = includeInClipboardHistory,
                    IsRoamable = includeInClipboardHistory
                });
            Clipboard.Flush();
        });
    }

    /// <summary>
    /// Performs the post-completion action (copy, append, replace) after a command finishes.
    /// </summary>
    internal static async Task PerformPostCompletionActionAsync(
        ActionOnCommandCompleted action,
        string output,
        WindowTextSelection? source,
        IProcessInteractionService processInteractionService)
    {
        if (action == ActionOnCommandCompleted.None)
        {
            return;
        }

        await ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            var clipboardBackup = new Dictionary<string, object>();
            if (action == ActionOnCommandCompleted.AppendSelection || action == ActionOnCommandCompleted.ReplaceSelection)
            {
                DataPackageView clipboardContent = Clipboard.GetContent();
                foreach (string? format in clipboardContent.AvailableFormats)
                {
                    try
                    {
                        clipboardBackup[format] = await clipboardContent.GetDataAsync(format);
                    }
                    catch
                    {
                    }
                }
            }

            try
            {
                await CopyAsync(output, includeInClipboardHistory: action == ActionOnCommandCompleted.Copy);

                await Task.Delay(200);

                if (source is not null
                    && (action == ActionOnCommandCompleted.AppendSelection
                        || action == ActionOnCommandCompleted.ReplaceSelection))
                {
                    if (action == ActionOnCommandCompleted.AppendSelection)
                    {
                        await processInteractionService.SimulateKeysOnWindow(
                            source,
                            [
                                VirtualKey.Right,
                                VirtualKey.Enter,
                                VirtualKey.Enter,
                            ]);

                        await Task.Delay(200);
                    }

                    await processInteractionService.SimulateKeysOnWindow(
                        source,
                        [
                            VirtualKey.LeftControl,
                            VirtualKey.V
                        ]);

                    await Task.Delay(200);
                }
            }
            catch
            {
            }
            finally
            {
                if (action == ActionOnCommandCompleted.AppendSelection || action == ActionOnCommandCompleted.ReplaceSelection)
                {
                    var dataPackage = new DataPackage();
                    foreach (KeyValuePair<string, object> item in clipboardBackup)
                    {
                        try
                        {
                            dataPackage.SetData(item.Key, item.Value);
                        }
                        catch
                        {
                        }
                    }

                    Clipboard.SetContent(dataPackage);
                }
            }
        });
    }
}
