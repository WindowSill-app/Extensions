using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;
using WindowSill.API;

namespace WindowSill.ClipboardHistory.ViewModels;

internal sealed partial class TextItemViewModel : ClipboardHistoryItemViewModelBase
{
    private readonly ILogger _logger;
    private readonly ISettingsProvider _settingsProvider;

    /// <summary>
    /// Gets or sets the truncated display text shown in the list item.
    /// </summary>
    [ObservableProperty]
    public partial string DisplayText { get; set; } = string.Empty;

    /// <inheritdoc />
    protected override DetectedClipboardDataType DetectedDataType => DetectedClipboardDataType.Text;

    /// <summary>
    /// Gets or sets the full text shown in the preview flyout.
    /// </summary>
    [ObservableProperty]
    public partial string PreviewText { get; set; } = string.Empty;

    internal TextItemViewModel(ISettingsProvider settingsProvider, IProcessInteractionService processInteractionService, ClipboardHistoryItem item)
        : base(processInteractionService, item)
    {
        _logger = this.Log();
        _settingsProvider = settingsProvider;

        InitializeAsync().Forget();
    }

    private async Task InitializeAsync()
    {
        await Task.Run(async () =>
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

                await ThreadHelper.RunOnUIThreadAsync(() =>
                {
                    if (_settingsProvider.GetSetting(Settings.Settings.HidePasswords)
                        && IsPassword(text))
                    {
                        DisplayText = new string('•', text.Length);
                    }
                    else
                    {
                        DisplayText
                            = text
                            .Substring(0, Math.Min(text.Length, 256))
                            .Trim()
                            .Replace("\r\n", "⏎")
                            .Replace("\n\r", "⏎")
                            .Replace('\r', '⏎')
                            .Replace('\n', '⏎');
                    }

                    PreviewText = text;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to initialize {nameof(TextItemViewModel)} control.");
            }
        });
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
