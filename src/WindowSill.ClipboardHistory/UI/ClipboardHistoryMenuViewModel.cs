using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;
using WindowSill.API;
using WindowSill.ClipboardHistory.Settings;

namespace WindowSill.ClipboardHistory.UI;

/// <summary>
/// ViewModel for the clipboard history menu flyout that contains all clipboard items.
/// </summary>
internal sealed partial class ClipboardHistoryMenuViewModel : ObservableObject
{
    private readonly ILogger _logger;
    private readonly ISettingsProvider _settingsProvider;
    private readonly IProcessInteractionService _processInteractionService;
    private readonly MenuFlyout _menuFlyout = new();

    internal ClipboardHistoryMenuViewModel(
        ILogger logger,
        ISettingsProvider settingsProvider,
        IProcessInteractionService processInteractionService)
    {
        _logger = logger;
        _settingsProvider = settingsProvider;
        _processInteractionService = processInteractionService;
    }

    /// <summary>
    /// Updates the menu flyout with the current clipboard history items.
    /// </summary>
    internal async Task UpdateMenuItemsAsync(IReadOnlyList<ClipboardHistoryItem> clipboardItems)
    {
        await ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            _menuFlyout.Items.Clear();

            if (clipboardItems.Count == 0)
            {
                var emptyItem = new MenuFlyoutItem
                {
                    Text = "/WindowSill.ClipboardHistory/Misc/EmptyClipboard".GetLocalizedString(),
                    IsEnabled = false
                };
                _menuFlyout.Items.Add(emptyItem);
                return;
            }

            // Add clipboard items to menu
            foreach (ClipboardHistoryItem item in clipboardItems)
            {
                try
                {
                    string displayText = await GetDisplayTextForItemAsync(item);
                    
                    var menuItem = new MenuFlyoutItem
                    {
                        Text = displayText,
                        Command = PasteItemCommand,
                        CommandParameter = item
                    };

                    // Add sub-menu for additional actions
                    var subMenu = new MenuFlyoutSubItem
                    {
                        Text = displayText
                    };

                    subMenu.Items.Add(new MenuFlyoutItem
                    {
                        Text = "/WindowSill.ClipboardHistory/Misc/Paste".GetLocalizedString(),
                        Icon = new SymbolIcon(Symbol.Paste),
                        Command = PasteItemCommand,
                        CommandParameter = item
                    });

                    subMenu.Items.Add(new MenuFlyoutItem
                    {
                        Text = "/WindowSill.ClipboardHistory/Misc/Delete".GetLocalizedString(),
                        Icon = new SymbolIcon(Symbol.Delete),
                        Command = DeleteItemCommand,
                        CommandParameter = item
                    });

                    _menuFlyout.Items.Add(subMenu);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create menu item for clipboard history item.");
                }
            }

            // Add separator and Clear History option at the bottom
            if (clipboardItems.Count > 0)
            {
                _menuFlyout.Items.Add(new MenuFlyoutSeparator());
                _menuFlyout.Items.Add(new MenuFlyoutItem
                {
                    Text = "/WindowSill.ClipboardHistory/Misc/ClearHistory".GetLocalizedString(),
                    Icon = new SymbolIcon(Symbol.Clear),
                    Command = ClearHistoryCommand
                });
            }
        });
    }

    /// <summary>
    /// Gets display text for a clipboard item based on its data type.
    /// </summary>
    private async Task<string> GetDisplayTextForItemAsync(ClipboardHistoryItem item)
    {
        try
        {
            DataPackageView data = item.Content;
            DetectedClipboardDataType dataType = await DataHelper.GetDetectedClipboardDataTypeAsync(item);

            switch (dataType)
            {
                case DetectedClipboardDataType.Text:
                    string? text = null;
                    if (data.AvailableFormats.Contains(StandardDataFormats.Text))
                    {
                        text = await data.GetTextAsync();
                    }
                    
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        return "/WindowSill.ClipboardHistory/Misc/EmptyText".GetLocalizedString();
                    }

                    // Check if it's a password and hide if needed
                    if (_settingsProvider.GetSetting(Settings.Settings.HidePasswords) && IsPassword(text))
                    {
                        return new string('•', Math.Min(text.Length, 20));
                    }

                    return text
                        .Substring(0, Math.Min(text.Length, 50))
                        .Trim()
                        .Replace("\r\n", " ")
                        .Replace("\n", " ")
                        .Replace("\r", " ");

                case DetectedClipboardDataType.Image:
                    return "🖼️ Image";

                case DetectedClipboardDataType.File:
                    return "📁 File(s)";

                case DetectedClipboardDataType.Uri:
                    if (data.AvailableFormats.Contains(StandardDataFormats.WebLink))
                    {
                        var uri = await data.GetWebLinkAsync();
                        return uri?.ToString().Substring(0, Math.Min(uri.ToString().Length, 50)) ?? "🔗 Link";
                    }
                    return "🔗 Link";

                case DetectedClipboardDataType.Color:
                    return "🎨 Color";

                case DetectedClipboardDataType.Html:
                    return "📄 HTML";

                case DetectedClipboardDataType.Rtf:
                    return "📝 RTF";

                case DetectedClipboardDataType.ApplicationLink:
                    return "📱 App Link";

                case DetectedClipboardDataType.UserActivity:
                    return "⚡ Activity";

                default:
                    return "/WindowSill.ClipboardHistory/Misc/UnsupportedFormat".GetLocalizedString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get display text for clipboard item.");
            return "/WindowSill.ClipboardHistory/Misc/UnsupportedFormat".GetLocalizedString();
        }
    }

    private static bool IsPassword(string text)
    {
        if (!string.IsNullOrEmpty(text) && text.Length >= 8 && text.Length <= 128)
        {
            bool hasUpper = false;
            bool hasLower = false;
            bool hasDigit = false;
            bool hasSpecial = false;

            string specials = "#?!@$%^&*-+";

            foreach (char c in text)
            {
                if (char.IsUpper(c))
                {
                    hasUpper = true;
                }
                else if (char.IsLower(c))
                {
                    hasLower = true;
                }
                else if (char.IsDigit(c))
                {
                    hasDigit = true;
                }
                else if (specials.IndexOf(c) >= 0)
                {
                    hasSpecial = true;
                }
                else
                {
                    return false;
                }
            }

            return hasUpper && hasLower && hasDigit && hasSpecial;
        }

        return false;
    }

    [RelayCommand]
    private async Task PasteItemAsync(ClipboardHistoryItem item)
    {
        await ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            Clipboard.SetHistoryItemAsContent(item);
            await _processInteractionService.SimulateKeysOnLastActiveWindow(
                Windows.System.VirtualKey.LeftControl,
                Windows.System.VirtualKey.V);
        });
    }

    [RelayCommand]
    private async Task DeleteItemAsync(ClipboardHistoryItem item)
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            Clipboard.DeleteItemFromHistory(item);
        });
    }

    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            Clipboard.ClearHistory();
        });
    }

    internal MenuFlyout GetMenuFlyout() => _menuFlyout;
}
