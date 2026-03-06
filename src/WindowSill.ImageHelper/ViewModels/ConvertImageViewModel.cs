using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ImageMagick;

using Windows.Storage;

using WindowSill.API;
using WindowSill.ImageHelper.Core;

namespace WindowSill.ImageHelper.ViewModels;

/// <summary>
/// ViewModel for the image conversion popup.
/// </summary>
internal sealed partial class ConvertImageViewModel : ObservableObject
{
    private readonly Action _closePopup;
    private readonly IImageConverter _converter;

    private CancellationTokenSource _cancellationTokenSource = new();

    internal ConvertImageViewModel(IReadOnlyList<IStorageFile> files, IImageConverter converter, Action closePopup)
    {
        Files = files;
        _converter = converter;
        _closePopup = closePopup;
        CancellationToken = _cancellationTokenSource.Token;
    }

    /// <summary>
    /// Gets the files to convert.
    /// </summary>
    internal IReadOnlyList<IStorageFile> Files { get; }

    /// <summary>
    /// Gets the image converter service.
    /// </summary>
    internal IImageConverter Converter => _converter;

    /// <summary>
    /// Gets or sets the selected target format.
    /// </summary>
    internal MagickFormat SelectedFormat { get; set; }

    /// <summary>
    /// Gets or sets the cancellation token for conversion operations.
    /// </summary>
    internal CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Gets the collection of conversion task items.
    /// </summary>
    public ObservableCollection<ConversionTaskItem> ConversionTasks { get; } = new();

    /// <summary>
    /// Gets or sets the cancel/done button text.
    /// </summary>
    [ObservableProperty]
    public partial string CancelButtonText { get; set; } = "/WindowSill.ImageHelper/ConvertImage/Cancel".GetLocalizedString();

    /// <summary>
    /// Selects a target format and signals for navigation to the progress page.
    /// </summary>
    /// <param name="formatName">The target image format name (e.g., "Png", "Jpeg").</param>
    [RelayCommand]
    private void Convert(string formatName)
    {
        SelectedFormat = Enum.Parse<MagickFormat>(formatName);
        FormatSelected?.Invoke(this, SelectedFormat);
    }

    /// <summary>
    /// Cancels the conversion and closes the popup.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _closePopup();
    }

    /// <summary>
    /// Raised when a format is selected, signaling the View to navigate to the progress page.
    /// </summary>
    internal event EventHandler<MagickFormat>? FormatSelected;

    /// <summary>
    /// Resets state when the popup closes.
    /// </summary>
    internal void OnClosing()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
        CancellationToken = _cancellationTokenSource.Token;
    }

    /// <summary>
    /// Starts the conversion tasks for all files.
    /// </summary>
    internal async Task RunConversionAsync(CancellationToken cancellationToken)
    {
        CancelButtonText = "/WindowSill.ImageHelper/ConvertImage/Cancel".GetLocalizedString();

        ConversionTasks.Clear();
        for (int i = 0; i < Files.Count; i++)
        {
            ConversionTasks.Add(new ConversionTaskItem(Files[i], SelectedFormat, _converter));
        }

        try
        {
            for (int i = 0; i < ConversionTasks.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await ConversionTasks[i].ConvertAsync(cancellationToken);
            }
        }
        catch (Exception)
        {
            // Conversion errors are handled per-task.
        }

        CancelButtonText = "/WindowSill.ImageHelper/ConvertImage/Done".GetLocalizedString();
    }
}
