using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;
using WindowSill.API;

namespace WindowSill.ClipboardHistory.ViewModels;

/// <summary>
/// ViewModel for clipboard history items containing user activity data.
/// </summary>
internal sealed partial class UserActivityItemViewModel : ClipboardHistoryItemViewModelBase
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserActivityItemViewModel"/> class.
    /// </summary>
    /// <param name="processInteractionService">Service for interacting with external processes.</param>
    /// <param name="item">The clipboard history item containing user activity data.</param>
    internal UserActivityItemViewModel(IProcessInteractionService processInteractionService, ClipboardHistoryItem item)
        : base(processInteractionService, item)
    {
        _logger = this.Log();
        InitializeAsync().Forget();
    }

    [ObservableProperty]
    public partial string DisplayText { get; set; } = string.Empty;

    private async Task InitializeAsync()
    {
        try
        {
            Guard.IsNotNull(Data);

            // Get the user activity JSON data
            string userActivityJson = await Data.GetDataAsync(StandardDataFormats.UserActivityJsonArray) as string ?? string.Empty;

            // For display, show a truncated version of the JSON or extract meaningful info
            DisplayText = string.IsNullOrEmpty(userActivityJson)
                ? "User Activity Data"
                : userActivityJson.Substring(0, Math.Min(userActivityJson.Length, 100)).Trim();

            // Clean up display text for better readability
            if (!string.IsNullOrEmpty(DisplayText))
            {
                DisplayText = DisplayText
                    .Replace("\r\n", " ")
                    .Replace("\n\r", " ")
                    .Replace('\r', ' ')
                    .Replace('\n', ' ')
                    .Replace("  ", " ")
                    .Trim();

                if (DisplayText.Length > 50)
                {
                    DisplayText = DisplayText.Substring(0, 50) + "...";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to initialize {nameof(UserActivityItemViewModel)} control.");
            DisplayText = "User Activity Data";
        }
    }
}
