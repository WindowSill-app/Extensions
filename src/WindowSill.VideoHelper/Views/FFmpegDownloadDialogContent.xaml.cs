using WindowSill.API;
using WindowSill.VideoHelper.Core;

namespace WindowSill.VideoHelper.Views;

/// <summary>
/// Dialog content for prompting the user to download FFmpeg.
/// </summary>
internal sealed partial class FFmpegDownloadDialogContent : UserControl
{
    private ContentDialog? _parentDialog;
    private CancellationTokenSource? _downloadCts;
    private string _ffmpegDirectory = string.Empty;
    private TaskCompletionSource<bool>? _downloadCompletionSource;

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
        dialog.CloseButtonClick += dialogContent.OnCloseButtonClick;

        _ = await dialog.ShowAsync();

        return dialogContent._downloadCompletionSource?.Task.Result ?? false;
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true;
        _ = StartDownloadAsync();
    }

    private void OnCloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        _downloadCts?.Cancel();

        if (!string.IsNullOrEmpty(_ffmpegDirectory))
        {
            FFmpegManager.CleanupPartialDownload(_ffmpegDirectory);
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

        PromptPanel.Visibility = Visibility.Collapsed;
        DownloadingPanel.Visibility = Visibility.Visible;
        ErrorInfoBar.IsOpen = false;

        _parentDialog.IsPrimaryButtonEnabled = false;

        _downloadCts = new CancellationTokenSource();
        var progress = new Progress<double>(UpdateProgress);

        try
        {
            await FFmpegManager.DownloadFFmpegAsync(_ffmpegDirectory, progress, _downloadCts.Token);
            _downloadCompletionSource.TrySetResult(true);
            _parentDialog.Hide();
        }
        catch (OperationCanceledException)
        {
            _downloadCompletionSource.TrySetResult(false);
            _parentDialog.Hide();
        }
        catch (Exception) when (!_downloadCts.IsCancellationRequested)
        {
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
            _parentDialog.IsPrimaryButtonEnabled = true;
        }
    }
}
