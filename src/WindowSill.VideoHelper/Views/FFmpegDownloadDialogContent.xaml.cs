using WindowSill.API;
using WindowSill.VideoHelper.Core;

namespace WindowSill.VideoHelper.Views;

/// <summary>
/// Dialog content for prompting the user to download FFmpeg.
/// </summary>
internal sealed partial class FFmpegDownloadDialogContent : UserControl
{
    /// <summary>
    /// Completed exactly once, with <see langword="true"/> only when FFmpeg was downloaded
    /// successfully. It is created up-front (rather than when the download starts) so that closing
    /// the dialog through any route — Close button, Escape, light dismiss or <c>Hide()</c> — can
    /// always complete it and release the caller.
    /// </summary>
    private readonly TaskCompletionSource<bool> _downloadCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private ContentDialog? _parentDialog;
    private CancellationTokenSource? _downloadCts;
    private string _ffmpegDirectory = string.Empty;

    private FFmpegDownloadDialogContent()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows the FFmpeg download dialog and handles the download process.
    /// </summary>
    /// <param name="xamlRoot">The XamlRoot to host the dialog.</param>
    /// <param name="pluginInfo">The plugin info for data folder access.</param>
    /// <returns>True if FFmpeg was downloaded successfully or already exists; false if the user cancelled.</returns>
    internal static async Task<bool> ShowAndDownloadAsync(XamlRoot xamlRoot, IPluginInfo pluginInfo)
    {
        string ffmpegDirectory = FFmpegManager.GetFFmpegDirectory(pluginInfo.GetPluginDataFolder());

        if (FFmpegManager.FFmpegExists(ffmpegDirectory))
        {
            return true;
        }

        var dialogContent = new FFmpegDownloadDialogContent
        {
            _ffmpegDirectory = ffmpegDirectory,
        };

        var dialog = new ContentDialog
        {
            Title = "/WindowSill.VideoHelper/FFmpegDownload/DialogTitle".GetLocalizedString(),
            PrimaryButtonText = "/WindowSill.VideoHelper/FFmpegDownload/DownloadButton".GetLocalizedString(),
            CloseButtonText = "/WindowSill.VideoHelper/FFmpegDownload/CancelButton".GetLocalizedString(),
            DefaultButton = ContentDialogButton.Primary,
            PrimaryButtonStyle = Application.Current.Resources["AccentButtonStyle"] as Style,
            XamlRoot = xamlRoot,
            Content = dialogContent,
        };

        dialogContent._parentDialog = dialog;

        dialog.PrimaryButtonClick += dialogContent.OnPrimaryButtonClick;
        dialog.Closed += dialogContent.OnDialogClosed;

        _ = await dialog.ShowAsync();

        return await dialogContent._downloadCompletionSource.Task;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true;
        StartDownloadAsync().ForgetSafely();
    }

    private void OnDialogClosed(ContentDialog sender, ContentDialogClosedEventArgs args)
    {
        // Unlike CloseButtonClick, Closed is raised for every dismissal route, including Escape and
        // light dismiss. TrySetResult only succeeds when the download has not already reported its
        // own outcome, so a successful download is never mistaken for a cancellation (and its
        // freshly downloaded binaries are never deleted below).
        if (_downloadCompletionSource.TrySetResult(false))
        {
            _downloadCts?.Cancel();

            if (!string.IsNullOrEmpty(_ffmpegDirectory))
            {
                FFmpegManager.CleanupPartialDownload(_ffmpegDirectory);
            }
        }
    }

    private async Task StartDownloadAsync()
    {
        if (_parentDialog is null)
        {
            return;
        }

        PromptPanel.Visibility = Visibility.Collapsed;
        DownloadingPanel.Visibility = Visibility.Visible;
        ErrorInfoBar.IsOpen = false;

        _parentDialog.IsPrimaryButtonEnabled = false;

        _downloadCts = new CancellationTokenSource();
        CancellationToken cancellationToken = _downloadCts.Token;
        var progress = new Progress<double>(UpdateProgress);

        try
        {
            await FFmpegManager.DownloadFFmpegAsync(_ffmpegDirectory, progress, cancellationToken);
            _downloadCompletionSource.TrySetResult(true);
            _parentDialog.Hide();
        }
        catch (OperationCanceledException)
        {
            _downloadCompletionSource.TrySetResult(false);
            _parentDialog.Hide();
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Leave the dialog open so the user can retry. The completion source stays pending on
            // purpose; whichever way the user then dismisses the dialog, OnDialogClosed completes it.
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
            _parentDialog.IsPrimaryButtonEnabled = true;
        }
    }
}
