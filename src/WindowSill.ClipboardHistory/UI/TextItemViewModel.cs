using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;
using WindowSill.API;

namespace WindowSill.ClipboardHistory.UI;

internal sealed class TextItemViewModel : ClipboardHistoryItemViewModelBase
{
    private readonly ILogger _logger;
    private readonly SillListViewButtonItem _view;
    private readonly ISettingsProvider _settingsProvider;

    private TextItemViewModel(ISettingsProvider settingsProvider, IProcessInteractionService processInteractionService, ClipboardHistoryItem item)
        : base(processInteractionService, item)
    {
        _logger = this.Log();
        _settingsProvider = settingsProvider;
        _view = new SillListViewButtonItem(base.PasteCommand);
        _view.DataContext = this;

        InitializeAsync().Forget();
    }

    internal static (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateView(ISettingsProvider settingsProvider, IProcessInteractionService processInteractionService, ClipboardHistoryItem item)
    {
        var viewModel = new TextItemViewModel(settingsProvider, processInteractionService, item);
        return (viewModel, viewModel._view);
    }

    private async Task InitializeAsync()
    {
        try
        {
            Guard.IsNotNull(Data);
            string? text = null;

            if (Data.AvailableFormats.Contains(StandardDataFormats.Text))
            {
                text = await Data.GetTextAsync();
            }
            else if (Data.AvailableFormats.Contains("AnsiText"))
            {
                text = await Data.GetDataAsync("AnsiText") as string;
            }
            else if (Data.AvailableFormats.Contains("OEMText"))
            {
                text = await Data.GetDataAsync("OEMText") as string;
            }
            else if (Data.AvailableFormats.Contains("TEXT"))
            {
                text = await Data.GetDataAsync("TEXT") as string;
            }

            text ??= string.Empty;

            if (_settingsProvider.GetSetting(Settings.Settings.HidePasswords)
                && IsPassword(text))
            {
                _view.Content = new string('•', text.Length);
            }
            else
            {
                _view.Content
                    = text
                    .Substring(0, Math.Min(text.Length, 256))
                    .Trim()
                    .Replace("\r\n", "⏎")
                    .Replace("\n\r", "⏎")
                    .Replace('\r', '⏎')
                    .Replace('\n', '⏎');
            }

            _view.PreviewFlyoutContent = text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to initialize {nameof(TextItemViewModel)} control.");
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

            // Allowed characters
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
                    return false; // invalid character found
                }
            }

            return hasUpper && hasLower && hasDigit && hasSpecial;
        }

        return false;
    }
}
