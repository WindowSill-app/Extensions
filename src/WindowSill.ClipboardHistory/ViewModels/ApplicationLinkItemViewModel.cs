using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using WindowSill.API;

namespace WindowSill.ClipboardHistory.ViewModels;

/// <summary>
/// ViewModel for clipboard history items containing application link data.
/// </summary>
internal sealed partial class ApplicationLinkItemViewModel : ClipboardHistoryItemViewModelBase
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationLinkItemViewModel"/> class.
    /// </summary>
    /// <param name="processInteractionService">Service for interacting with external processes.</param>
    /// <param name="item">The clipboard history item containing application link data.</param>
    internal ApplicationLinkItemViewModel(IProcessInteractionService processInteractionService, ClipboardHistoryItem item)
        : base(processInteractionService, item)
    {
        _logger = this.Log();
        InitializeAsync().Forget();
    }

    [ObservableProperty]
    public partial string ApplicationLink { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DisplayText { get; set; } = string.Empty;

    [RelayCommand]
    private async Task OpenApplicationAsync()
    {
        if (Uri.TryCreate(ApplicationLink, UriKind.Absolute, out Uri? uri))
        {
            await Launcher.LaunchUriAsync(uri);
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            Guard.IsNotNull(Data);

            // Get the application link data
            Uri appLink = await Data.GetApplicationLinkAsync();
            ApplicationLink = appLink?.ToString() ?? string.Empty;
            DisplayText = ApplicationLink;

            // Try to get a better display text from plain text if available
            if (Data.Contains(StandardDataFormats.Text))
            {
                string text = await Data.GetTextAsync();
                if (!string.IsNullOrEmpty(text) && text != ApplicationLink)
                {
                    DisplayText = text;
                }
            }

            // Fallback display text
            if (string.IsNullOrEmpty(DisplayText))
            {
                DisplayText = "Application Link";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to initialize {nameof(ApplicationLinkItemViewModel)} control.");
            ApplicationLink = string.Empty;
            DisplayText = "Application Link";
        }
    }
}
