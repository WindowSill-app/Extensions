using WindowSill.API;
using WindowSill.TextFinder.Core.MeaningSimilarity;

namespace WindowSill.TextFinder.Views;

/// <summary>
/// Dialog content for prompting the user to download the meaning similarity model.
/// </summary>
public sealed partial class ModelDownloadDialogContent : UserControl
{
    /// <summary>
    /// Completed exactly once, with <see langword="true"/> only when the model was downloaded
    /// successfully. It is created up-front (rather than when the download starts) so that closing
    /// the dialog through any route — Close button, Escape, light dismiss or <c>Hide()</c> — can
    /// always complete it and release the caller.
    /// </summary>
    private readonly TaskCompletionSource<bool> _downloadCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private ContentDialog? _parentDialog;
    private CancellationTokenSource? _downloadCts;
    private string _modelDirectory = string.Empty;

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
        dialog.Closed += dialogContent.OnDialogClosed;

        _ = await dialog.ShowAsync();

        // Return the result from download completion. This must never be awaited synchronously:
        // the download resumes on the UI thread, so blocking it here would deadlock permanently.
        return await dialogContent._downloadCompletionSource.Task;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Prevent dialog from closing
        args.Cancel = true;

        // Start download
        StartDownloadAsync().ForgetSafely();
    }

    private void OnDialogClosed(ContentDialog sender, ContentDialogClosedEventArgs args)
    {
        // Unlike CloseButtonClick, Closed is raised for every dismissal route, including Escape and
        // light dismiss. TrySetResult only succeeds when the download has not already reported its
        // own outcome, so a successful download is never mistaken for a cancellation (and its
        // freshly downloaded files are never deleted below).
        if (_downloadCompletionSource.TrySetResult(false))
        {
            _downloadCts?.Cancel();

            if (!string.IsNullOrEmpty(_modelDirectory))
            {
                ModelManager.CleanupPartialDownload(_modelDirectory);
            }
        }
    }

    private async Task StartDownloadAsync()
    {
        if (_parentDialog is null)
        {
            return;
        }

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
            // Show error and allow retry. The completion source stays pending on purpose; whichever
            // way the user then dismisses the dialog, OnDialogClosed completes it.
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
        }).ForgetSafely();
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
