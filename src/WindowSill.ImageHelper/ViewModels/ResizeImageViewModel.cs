using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ImageMagick;

using Windows.Storage;

using WindowSill.API;
using WindowSill.ImageHelper.Core;
using WindowSill.ImageHelper.Helpers;

namespace WindowSill.ImageHelper.ViewModels;

/// <summary>
/// ViewModel for the image resize popup.
/// </summary>
internal sealed partial class ResizeImageViewModel : ObservableObject
{
    private readonly IStorageFile _file;
    private readonly IImageResizer _resizer;
    private readonly Action _closePopup;

    private uint _originalWidth;
    private uint _originalHeight;
    private bool _userIsChangingWidth;
    private bool _userIsChangingHeight;

    internal ResizeImageViewModel(IStorageFile file, IImageResizer resizer, Action closePopup)
    {
        _file = file;
        _resizer = resizer;
        _closePopup = closePopup;
    }

    /// <summary>
    /// Gets or sets the resize mode (absolute or percentage).
    /// </summary>
    [ObservableProperty]
    public partial ResizeMode ResizeMode { get; set; }

    /// <summary>
    /// Gets or sets the target width in pixels.
    /// </summary>
    [ObservableProperty]
    public partial uint Width { get; set; }

    /// <summary>
    /// Gets or sets the target height in pixels.
    /// </summary>
    [ObservableProperty]
    public partial uint Height { get; set; }

    /// <summary>
    /// Gets or sets whether to maintain the aspect ratio during resize.
    /// </summary>
    [ObservableProperty]
    public partial bool MaintainAspectRatio { get; set; } = true;

    /// <summary>
    /// Gets or sets the resize percentage.
    /// </summary>
    [ObservableProperty]
    public partial int Percentage { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether a resize operation is currently in progress.
    /// </summary>
    [ObservableProperty]
    public partial bool IsResizeInProgress { get; set; }

    partial void OnHeightChanging(uint value)
    {
        if (MaintainAspectRatio && !_userIsChangingWidth)
        {
            _userIsChangingHeight = true;
            Width = value * _originalWidth / _originalHeight;
        }

        _userIsChangingHeight = false;
    }

    partial void OnWidthChanging(uint value)
    {
        if (MaintainAspectRatio && !_userIsChangingHeight)
        {
            _userIsChangingWidth = true;
            Height = value * _originalHeight / _originalWidth;
        }

        _userIsChangingWidth = false;
    }

    partial void OnMaintainAspectRatioChanged(bool value)
    {
        if (MaintainAspectRatio)
        {
            Height = _originalHeight * Width / _originalWidth;
        }
    }

    /// <summary>
    /// Initiates the resize operation.
    /// </summary>
    [RelayCommand]
    private void Resize()
    {
        IsResizeInProgress = true;

        ResizeAsync().ContinueWith(task =>
        {
            ThreadHelper.RunOnUIThreadAsync(() =>
            {
                IsResizeInProgress = false;
                if (!task.IsFaulted)
                {
                    _closePopup();
                }
            });
        });
    }

    /// <summary>
    /// Loads the original image dimensions when the popup opens.
    /// </summary>
    internal void OnOpening()
    {
        try
        {
            (uint width, uint height) = _resizer.GetDimensions(_file.Path);
            _originalWidth = width;
            _originalHeight = height;
            Width = _originalWidth;
            Height = _originalHeight;
        }
        catch (Exception)
        {
            // Failed to read image dimensions.
        }
    }

    internal async Task ResizeAsync()
    {
        MagickGeometry newSize;

        switch (ResizeMode)
        {
            case ResizeMode.AbsoluteSize:
                newSize = new MagickGeometry(Width, Height)
                {
                    IgnoreAspectRatio = !MaintainAspectRatio
                };
                break;

            case ResizeMode.Percentage:
                uint newWidth = (uint)(_originalWidth * Percentage / 100.0);
                uint newHeight = (uint)(_originalHeight * Percentage / 100.0);
                newSize = new MagickGeometry(newWidth, newHeight)
                {
                    IgnoreAspectRatio = false
                };
                break;

            default:
                ThrowHelper.ThrowNotSupportedException();
                return;
        }

        string outputPath = FilePathHelper.GetUniqueOutputPath(_file.Path, "_resized");
        await _resizer.ResizeAsync(_file.Path, outputPath, newSize);
    }
}
