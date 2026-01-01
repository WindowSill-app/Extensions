using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageMagick;
using ImageMagick.Factories;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using WindowSill.API;
using ZXing;
using ZXing.Common;
using ZXing.QrCode.Internal;
using ZXing.Rendering;
using static ZXing.Rendering.SvgRenderer;

namespace WindowSill.URLHelper;

internal sealed partial class QRCodeSillPopup : ObservableObject
{
    private readonly ILogger _logger;
    private readonly WindowTextSelection _currentSelection;
    private readonly SillPopupContent _view;

    private IMagickImage<ushort>? _qrCodeImage;

    private QRCodeSillPopup(WindowTextSelection currentSelection)
    {
        _logger = this.Log();
        _currentSelection = currentSelection;
        _view = new SillPopupContent(OnOpening)
            .Width(350)
            .DataContext(
                this,
                (view, viewModel) => view
                .Content(
                    new Grid()
                        .RowDefinitions(
                            new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) },
                            new RowDefinition() { Height = GridLength.Auto }
                        )
                        .Children(
                            new Border()
                                .Grid(row: 0)
                                .Margin(24)
                                .CornerRadius(x => x.ThemeResource("ControlCornerRadius"))
                                .Child(
                                    new Image()
                                        .MinHeight(300)
                                        .MinWidth(300)
                                        .HorizontalAlignment(HorizontalAlignment.Stretch)
                                        .VerticalAlignment(VerticalAlignment.Stretch)
                                        .Stretch(Stretch.Uniform)
                                        .Source(x => x.Binding(() => viewModel.QRCodeImage).OneWay())
                                ),
                            new Border()
                                .Grid(row: 1)
                                .HorizontalAlignment(HorizontalAlignment.Stretch)
                                .VerticalAlignment(VerticalAlignment.Bottom)
                                .Padding(24)
                                .BorderThickness(0, 1, 0, 0)
                                .BorderBrush(x => x.ThemeResource("CardStrokeColorDefaultBrush"))
                                .Background(x => x.ThemeResource("LayerOnAcrylicFillColorDefaultBrush"))
                                .Child(
                                    new Grid()
                                        .ColumnSpacing(8)
                                        .ColumnDefinitions(
                                            new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) },
                                            new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) }
                                        )
                                        .Children(
                                            new Button()
                                                .Grid(column: 0)
                                                .Style(x => x.ThemeResource("LargeButtonStyle"))
                                                .Command(() => viewModel.CopyCommand)
                                                .Content(
                                                    new StackPanel()
                                                        .Spacing(8)
                                                        .Orientation(Orientation.Horizontal)
                                                        .Children(
                                                            new FontIcon().Glyph("\uE8C8"),
                                                            new TextBlock().Text("/WindowSill.URLHelper/QRCode/Copy".GetLocalizedString())
                                                        )
                                                ),
                                            new Button()
                                                .Grid(column: 1)
                                                .Style(x => x.ThemeResource("LargeButtonStyle"))
                                                .Command(() => viewModel.SaveCommand)
                                                .Content(
                                                    new StackPanel()
                                                        .Spacing(8)
                                                        .Orientation(Orientation.Horizontal)
                                                        .Children(
                                                            new FontIcon().Glyph("\uE74E"),
                                                            new TextBlock().Text("/WindowSill.URLHelper/QRCode/Save".GetLocalizedString())
                                                        )
                                                )
                                        )
                                )
                        )
                )
            );
    }

    internal static SillPopupContent CreateView(WindowTextSelection currentSelection)
    {
        var qrCodeSillPopupViewModel = new QRCodeSillPopup(currentSelection);
        return qrCodeSillPopupViewModel._view;
    }

    [ObservableProperty]
    public partial BitmapImage? QRCodeImage { get; set; }

    [RelayCommand]
    private void Copy()
    {
        string url = _currentSelection.SelectedText;
        if (!string.IsNullOrWhiteSpace(url))
        {
            ThreadHelper.RunOnUIThreadAsync(async () =>
            {
                try
                {
                    Guard.IsNotNull(_qrCodeImage);

                    // Encode to PNG
                    byte[] pngBytes;
                    using (var memStream = new MemoryStream())
                    {
                        _qrCodeImage.Format = MagickFormat.Png;
                        _qrCodeImage.Write(memStream);
                        pngBytes = memStream.ToArray();
                    }

                    // Copy to clipboard
                    using (var ras = new InMemoryRandomAccessStream())
                    {
                        await ras.WriteAsync(pngBytes.AsBuffer());
                        ras.Seek(0);

                        var dataPackage = new DataPackage();
                        dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(ras));
                        Clipboard.SetContent(dataPackage);
                        Clipboard.Flush();
                    }

                    _view.Close();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while trying to copy a QRCode to the clipboard.");
                }
            });
        }
    }

    [RelayCommand]
    private void Save()
    {
        string url = _currentSelection.SelectedText;
        if (!string.IsNullOrWhiteSpace(url))
        {
            ThreadHelper.RunOnUIThreadAsync(async () =>
            {
                string extension = "unknown";
                try
                {
                    // Create and configure FileSavePicker
                    var savePicker = new FileSavePicker
                    {
                        SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                        SuggestedFileName = "QRCode"
                    };

                    // Add file type choices
                    savePicker.FileTypeChoices.Add("PNG Image", [".png"]);
                    savePicker.FileTypeChoices.Add("JPEG Image", [".jpg", ".jpeg"]);
                    savePicker.FileTypeChoices.Add("WebP Image", [".webp"]);
                    savePicker.FileTypeChoices.Add("SVG Vector", [".svg"]);

                    // Initialize the picker with the window handle
                    // Get the window handle from the current XamlRoot
                    if (_view.XamlRoot?.ContentIslandEnvironment?.AppWindowId is { } windowId)
                    {
                        nint hwnd = Microsoft.UI.Win32Interop.GetWindowFromWindowId(windowId);
                        WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
                    }

                    // Show the picker
                    StorageFile file = await savePicker.PickSaveFileAsync();
                    if (file != null)
                    {
                        // Determine file format based on extension
                        extension = file.FileType.ToLowerInvariant();

                        if (extension == ".svg")
                        {
                            // Save as SVG
                            string svgContent = GenerateSvgQrCode(url);
                            await FileIO.WriteTextAsync(file, svgContent);
                        }
                        else
                        {
                            // Save as image format using ImageMagick
                            Guard.IsNotNull(_qrCodeImage);

                            MagickFormat format = extension switch
                            {
                                ".png" => MagickFormat.Png,
                                ".jpg" or ".jpeg" => MagickFormat.Jpeg,
                                ".webp" => MagickFormat.WebP,
                                _ => ThrowHelper.ThrowNotSupportedException<MagickFormat>()
                            };

                            byte[] imageBytes;
                            using (var memStream = new MemoryStream())
                            {
                                _qrCodeImage.Format = format;
                                _qrCodeImage.Write(memStream);
                                imageBytes = memStream.ToArray();
                            }

                            await FileIO.WriteBytesAsync(file, imageBytes);
                        }

                        _view.Close();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while trying to save a QRCode to a file as '{extension}'.", extension);
                }
            });
        }
    }

    private void OnOpening()
    {
        string url = _currentSelection.SelectedText;
        if (!string.IsNullOrWhiteSpace(url))
        {
            ThreadHelper.RunOnUIThreadAsync(() =>
            {
                try
                {
                    // Generate QR code as BitmapImage
                    _qrCodeImage = GenerateQrCode(url);
                    BitmapImage bitmapImage = ToBitmapImage(_qrCodeImage);
                    QRCodeImage = bitmapImage;
                }
                catch (Exception ex)
                {
                    QRCodeImage = null;
                    _logger.LogError(ex, "Error while trying to generate a QRCode.");
                }
            });
        }
    }
    private static BitmapImage ToBitmapImage(IMagickImage<ushort> image)
    {
        using MemoryStream stream = new();
        image.Write(stream, MagickFormat.Png);
        stream.Position = 0;
        BitmapImage bitmapImage = new();
        bitmapImage.SetSource(stream.AsRandomAccessStream());
        return bitmapImage;
    }

    private static IMagickImage<ushort> GenerateQrCode(string text)
    {
        var barcodeWriter = new ZXing.Magick.BarcodeWriter<ushort>(new MagickImageFactory())
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions
            {
                Height = 1024,
                Width = 1024,
                Margin = 2
            }
        };

        IMagickImage<ushort> image = barcodeWriter.Write(text);
        return image;
    }

    private static string GenerateSvgQrCode(string text)
    {
        BarcodeWriterSvg barcodeWriter = new()
        {
            Format = BarcodeFormat.QR_CODE,
            Renderer = new SvgRenderer()
        };

        EncodingOptions encodingOptions = new()
        {
            Width = 1024,
            Height = 1024,
            Margin = 2,
        };
        encodingOptions.Hints.Add(EncodeHintType.ERROR_CORRECTION, ErrorCorrectionLevel.M);
        barcodeWriter.Options = encodingOptions;

        SvgImage svg = barcodeWriter.Write(text);
        return svg.Content;
    }
}
