using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Extensions.Logging;

using Windows.Storage;

using WindowSill.API;
using WindowSill.ImageHelper.Core;

namespace WindowSill.ImageHelper.ViewModels;

/// <summary>
/// ViewModel for the image compression popup.
/// </summary>
internal sealed partial class CompressImageViewModel : ObservableObject
{
    private readonly IReadOnlyList<IStorageFile> _files;
    private readonly IImageCompressor _compressor;
    private readonly Action _closePopup;

    private CancellationTokenSource _cancellationTokenSource = new();

    internal CompressImageViewModel(IReadOnlyList<IStorageFile> files, IImageCompressor compressor, Action closePopup)
    {
        _files = files;
        _compressor = compressor;
        _closePopup = closePopup;
    }

    /// <summary>
    /// Gets the collection of compression task items.
    /// </summary>
    public ObservableCollection<CompressionTaskItem> CompressionTasks { get; } = new();

    /// <summary>
    /// Gets or sets the cancel/done button text.
    /// </summary>
    [ObservableProperty]
    public partial string ActionButtonText { get; set; } = string.Empty;

    /// <summary>
    /// Cancels the compression and closes the popup.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _closePopup();
    }

    /// <summary>
    /// Initializes the compression tasks when the popup opens.
    /// </summary>
    internal void OnOpening()
    {
        ActionButtonText = "/WindowSill.ImageHelper/CompressImage/Cancel".GetLocalizedString();

        CompressionTasks.Clear();
        for (int i = 0; i < _files.Count; i++)
        {
            if (string.IsNullOrEmpty(_files[i].Path))
            {
                continue;
            }

            CompressionTasks.Add(new CompressionTaskItem(_files[i], _compressor));
        }

        RunLosslessCompressionAsync(_cancellationTokenSource.Token).Forget();
    }

    /// <summary>
    /// Cancels pending operations when the popup closes.
    /// </summary>
    internal void OnClosing()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    private async Task RunLosslessCompressionAsync(CancellationToken cancellationToken)
    {
        try
        {
            CompressionTaskItem[] tasks = CompressionTasks.ToArray();
            for (int i = 0; i < tasks.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await tasks[i].CompressAsync();
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException || ex is TaskCanceledException)
        {
            // Cancellation is expected.
        }
        catch (Exception ex)
        {
            this.Log().LogError(ex, "Error while doing lossless image compression.");
        }

        ActionButtonText = "/WindowSill.ImageHelper/CompressImage/Done".GetLocalizedString();
    }
}
