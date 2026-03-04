using WindowSill.API;
using WindowSill.TextFinder.Core.MeaningSimilarity;

namespace WindowSill.TextFinder.Views;

/// <summary>
/// Dialog content for prompting the user to download the meaning similarity model.
/// </summary>
public sealed partial class ModelDownloadDialogContent : UserControl
{
    private ContentDialog? _parentDialog;
    private CancellationTokenSource? _downloadCts;
    private string _modelDirectory = string.Empty;
    private TaskCompletionSource<bool>? _downloadCompletionSource;

    private ModelDownloadDialogContent()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows the model download dialog and handles the download process.
    /// </summary>
    /// <param name="xamlRoot">The XamlRoot to host the dialog.</param>
    /// <param name="modelDirectory">The directory where the model should be downloaded.</param>
    /// <returns>True if the model was downloaded successfully or already exists; false if the user cancelled.</returns>
    public static async Task<bool> ShowAndDownloadAsync(XamlRoot xamlRoot, IPluginInfo pluginInfo)
    {
        string modelDirectory = ModelManager.GetModelDirectory(pluginInfo.GetPluginDataFolder());

        if (ModelManager.ModelExists(modelDirectory))
        {
            return true;
        }

        var dialogContent = new ModelDownloadDialogContent
        {
            _modelDirectory = modelDirectory
        };

        var dialog = new ContentDialog
        {
            Title = "/WindowSill.TextFinder/ModelDownloadDialogContent/DownloadRequired".GetLocalizedString(),
            PrimaryButtonText = "/WindowSill.TextFinder/ModelDownloadDialogContent/Download".GetLocalizedString(),
            CloseButtonText = "/WindowSill.TextFinder/ModelDownloadDialogContent/Cancel".GetLocalizedString(),
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonStyle = Application.Current.Resources["AccentButtonStyle"] as Style,
            XamlRoot = xamlRoot,
            Content = dialogContent
        };

        dialogContent._parentDialog = dialog;

        // Prevent dialog from closing when Download is clicked - we handle closing manually
        dialog.PrimaryButtonClick += dialogContent.OnPrimaryButtonClick;
        dialog.CloseButtonClick += dialogContent.OnCloseButtonClick;

        _ = await dialog.ShowAsync();

        // Return the result from download completion
        return dialogContent._downloadCompletionSource?.Task.Result ?? false;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Prevent dialog from closing
        args.Cancel = true;

        // Start download
        _ = StartDownloadAsync();
    }

    private void OnCloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Cancel any ongoing download
        _downloadCts?.Cancel();

        // Clean up any partially downloaded files
        if (!string.IsNullOrEmpty(_modelDirectory))
        {
            ModelManager.CleanupPartialDownload(_modelDirectory);
        }

        _downloadCompletionSource?.TrySetResult(false);
    }

    private async Task StartDownloadAsync()
    {
        if (_parentDialog is null)
        {
            return;
        }

        _downloadCompletionSource = new TaskCompletionSource<bool>();

        // Switch to downloading state
        PromptPanel.Visibility = Visibility.Collapsed;
        DownloadingPanel.Visibility = Visibility.Visible;
        ErrorInfoBar.IsOpen = false;

        // Disable Download button, keep Cancel available
        _parentDialog.IsPrimaryButtonEnabled = false;
        _parentDialog.IsSecondaryButtonEnabled = false;

        _downloadCts = new CancellationTokenSource();
        var progress = new Progress<double>(UpdateProgress);

        try
        {
            await ModelManager.DownloadModelAsync(_modelDirectory, progress, _downloadCts.Token);

            // Download completed successfully
            _downloadCompletionSource.TrySetResult(true);
            _parentDialog.Hide();
        }
        catch (OperationCanceledException)
        {
            // User cancelled
            _downloadCompletionSource.TrySetResult(false);
            _parentDialog.Hide();
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            // Show error and allow retry
            ShowError();
        }
    }

    private void UpdateProgress(double progressValue)
    {
        ThreadHelper.RunOnUIThreadAsync(() =>
        {
            int percentage = (int)(progressValue * 100);
            DownloadProgressBar.Value = percentage;
            ProgressTextBlock.Text = $"{percentage}%";
        });
    }

    private void ShowError()
    {
        DownloadingPanel.Visibility = Visibility.Collapsed;
        PromptPanel.Visibility = Visibility.Visible;
        ErrorInfoBar.IsOpen = true;

        if (_parentDialog is not null)
        {
            // Re-enable buttons for retry
            _parentDialog.IsPrimaryButtonEnabled = true;
            _parentDialog.PrimaryButtonText = "/WindowSill.TextFinder/ModelDownloadDialogContent/TryAgain".GetLocalizedString();
        }
    }
}
