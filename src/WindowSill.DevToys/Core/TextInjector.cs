using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using WindowSill.API;

namespace WindowSill.DevToys.Core;

internal class TextInjector
{
    private static readonly ILogger logger = typeof(TextInjector).Log();

    internal static async Task InjectAsync(string text, WindowTextSelection windowTextSelection)
    {
        await ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            var clipboardBackup = new Dictionary<string, object>();
            DataPackageView clipboardContent = Clipboard.GetContent();
            foreach (string? format in clipboardContent.AvailableFormats)
            {
                try
                {
                    clipboardBackup[format] = await clipboardContent.GetDataAsync(format);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while retrieving clipboard data for format {format}", format);
                }
            }

            try
            {
                // Make sure to place the parent window back top of all other windows and give it focus.
                var parentWindowHandle = new HWND(windowTextSelection.WindowHandle);
                if (PInvoke.SetForegroundWindow(parentWindowHandle))
                {
                    PInvoke.SetActiveWindow(parentWindowHandle);
                    PInvoke.SetFocus(parentWindowHandle);

                    // Set the new text to the clipboard.
                    var dataPackage = new DataPackage { RequestedOperation = DataPackageOperation.Move };
                    dataPackage.SetText(text);

                    Clipboard.SetContentWithOptions(
                        dataPackage,
                        new ClipboardContentOptions()
                        {
                            IsAllowedInHistory = false,
                            IsRoamable = false
                        });

                    await Task.Delay(200); // Wait for the clipboard to be set.

                    // Simulate Ctrl+V to paste the new text into the window.
                    SimulateKeys(
                        [
                            VirtualKey.LeftControl,
                            VirtualKey.V
                        ]);

                    await Task.Delay(200);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while copy/pasting LLM-generated text into foreground app.");
            }
            finally
            {
                // Restore the clipboard content.
                var dataPackage = new DataPackage();
                foreach (KeyValuePair<string, object> item in clipboardBackup)
                {
                    try
                    {
                        dataPackage.SetData(item.Key, item.Value);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error while restoring the clipboard content.");
                    }
                }

                Clipboard.SetContent(dataPackage);
            }
        });
    }

    private static unsafe void SimulateKeys(VirtualKey[] keys)
    {
        var keyDownInputs = new INPUT[keys.Length];
        var keyUpInputs = new INPUT[keys.Length];

        for (int j = 0; j < keys.Length; j++)
        {
            keyDownInputs[j] = CreateInputForKeyboardEvent(keys[j], isKeyUp: false);
            keyUpInputs[j] = CreateInputForKeyboardEvent(keys[j], isKeyUp: true);
        }

        PInvoke.SendInput(keyDownInputs.AsSpan(), sizeof(INPUT));
        PInvoke.SendInput(keyUpInputs.AsSpan(), sizeof(INPUT));
    }

    private static unsafe INPUT CreateInputForKeyboardEvent(VirtualKey key, bool isKeyUp)
    {
        const uint None = 0x0000; // Indicates Key Down event.

        var keyboardInput = new KEYBDINPUT()
        {
            wVk = (VIRTUAL_KEY)key,
            wScan = (ushort)(PInvoke.MapVirtualKey((ushort)key, MAP_VIRTUAL_KEY_TYPE.MAPVK_VK_TO_VSC) & 0xFFU),
            dwFlags = isKeyUp ? KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP : None
        };

        INPUT input = default;
        input.type = INPUT_TYPE.INPUT_KEYBOARD;
        input.Anonymous.ki = keyboardInput;

        return input;
    }
}
