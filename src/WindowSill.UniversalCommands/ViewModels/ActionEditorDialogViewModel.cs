using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using WindowSill.API;
using WindowSill.UniversalCommands.Core;

namespace WindowSill.UniversalCommands.ViewModels;

/// <summary>
/// View model for the action editor dialog, managing form state, validation, and result construction.
/// </summary>
internal sealed partial class ActionEditorDialogViewModel : ObservableObject
{
    private static List<string>? cachedGlyphs;

    private readonly ContentDialog _contentDialog;
    private readonly UniversalCommand _command;
    private readonly XamlRoot _xamlRoot;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionEditorDialogViewModel"/> class.
    /// </summary>
    /// <param name="contentDialog">The hosting content dialog, used to control primary button state.</param>
    /// <param name="command">The command to edit or a new default command.</param>
    internal ActionEditorDialogViewModel(ContentDialog contentDialog, UniversalCommand command)
    {
        _contentDialog = contentDialog;
        _command = command;
        _xamlRoot = contentDialog.XamlRoot;

        ActionName = command.Name;
        ActionTypeIndex = (int)command.Type;
        SelectedIconGlyph = command.IconGlyph;
        SelectedImagePath = command.IconImagePath;
        IconTypeIndex = string.IsNullOrEmpty(command.IconImagePath) ? 0 : 1;
        PowerShellCommand = command.PowerShellCommand ?? string.Empty;
        IsGlobal = command.TargetAppProcessName is null;
        TargetAppProcessName = command.TargetAppProcessName ?? string.Empty;
        RecordedChordInts = command.KeyboardChord.Select(c => c.ToList()).ToList();

        UpdateIconPreview();
        ValidateForm();

        IsLoadingGlyphs = true;
        _ = LoadGlyphsAsync();
    }

    [ObservableProperty]
    internal partial string ActionName { get; set; } = string.Empty;

    [ObservableProperty]
    internal partial int ActionTypeIndex { get; set; }

    [ObservableProperty]
    internal partial int IconTypeIndex { get; set; }

    [ObservableProperty]
    internal partial char SelectedIconGlyph { get; set; } = '\uE768';

    [ObservableProperty]
    internal partial string? SelectedImagePath { get; set; }

    [ObservableProperty]
    internal partial string PowerShellCommand { get; set; } = string.Empty;

    [ObservableProperty]
    internal partial bool IsGlobal { get; set; } = true;

    [ObservableProperty]
    internal partial string TargetAppProcessName { get; set; } = string.Empty;

    [ObservableProperty]
    internal partial string IconPreviewGlyphText { get; set; } = "\uE768";

    [ObservableProperty]
    internal partial bool ShowGlyphPreview { get; set; } = true;

    [ObservableProperty]
    internal partial bool ShowImagePreview { get; set; }

    [ObservableProperty]
    internal partial bool IsLoadingGlyphs { get; set; }

    [ObservableProperty]
    internal partial ImageSource? IconPreviewImageSource { get; set; }

    /// <summary>
    /// Gets or sets the discovered glyph list for the glyph picker.
    /// </summary>
    [ObservableProperty]
    internal partial List<string>? Glyphs { get; set; }

    /// <summary>
    /// Gets or sets the selected glyph index in the glyph picker grid.
    /// </summary>
    [ObservableProperty]
    internal partial int SelectedGlyphIndex { get; set; } = -1;

    /// <summary>
    /// Gets whether the current action type is a keyboard shortcut.
    /// </summary>
    internal bool IsShortcutType => ActionTypeIndex == 0;

    /// <summary>
    /// Gets whether the current action type is a PowerShell command.
    /// </summary>
    internal bool IsPowerShellType => ActionTypeIndex != 0;

    /// <summary>
    /// Gets whether the icon mode is glyph-based.
    /// </summary>
    internal bool IsGlyphMode => IconTypeIndex == 0;

    /// <summary>
    /// Gets whether the icon mode is image-based.
    /// </summary>
    internal bool IsImageMode => IconTypeIndex != 0;

    /// <summary>
    /// Gets whether the target app text box should be visible.
    /// </summary>
    internal bool IsTargetAppVisible => !IsGlobal;

    /// <summary>
    /// Gets the file name portion of the selected image path, or a placeholder.
    /// </summary>
    internal string ImageFileName => string.IsNullOrEmpty(SelectedImagePath)
        ? "/WindowSill.UniversalCommands/SettingsView/NoFileSelected".GetLocalizedString()
        : System.IO.Path.GetFileName(SelectedImagePath);

    /// <summary>
    /// Gets or sets the recorded keyboard chord as integer virtual-key codes.
    /// </summary>
    internal List<List<int>> RecordedChordInts { get; set; } = [];

    /// <summary>
    /// Updates the recorded keyboard chord and re-validates the form.
    /// </summary>
    /// <param name="chord">The chord as integer virtual-key codes.</param>
    internal void UpdateRecordedChord(List<List<int>> chord)
    {
        RecordedChordInts = chord;
        ValidateForm();
    }

    /// <summary>
    /// Opens a file picker to select a custom icon image.
    /// </summary>
    [RelayCommand]
    internal async Task BrowseImageAsync()
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".svg");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".webp");

        if (_xamlRoot.ContentIslandEnvironment?.AppWindowId is { } appWindowId)
        {
            nint hwnd = Microsoft.UI.Win32Interop.GetWindowFromWindowId(appWindowId);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        }

        StorageFile? file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            SelectedImagePath = file.Path;
        }
    }

    /// <summary>
    /// Rebuilds the icon preview state from current property values.
    /// </summary>
    internal void UpdateIconPreview()
    {
        if (!string.IsNullOrEmpty(SelectedImagePath) && IconTypeIndex == 1)
        {
            ShowGlyphPreview = false;
            ShowImagePreview = true;
            IconPreviewImageSource = SelectedImagePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
                ? new SvgImageSource(new Uri(SelectedImagePath))
                : new BitmapImage(new Uri(SelectedImagePath));
        }
        else
        {
            ShowGlyphPreview = true;
            ShowImagePreview = false;
            IconPreviewGlyphText = SelectedIconGlyph.ToString();
        }
    }

    /// <summary>
    /// Constructs a <see cref="UniversalCommand"/> from the current form state.
    /// </summary>
    /// <returns>The configured universal command.</returns>
    internal UniversalCommand ToUniversalCommand()
    {
        bool useImage = IconTypeIndex == 1 && !string.IsNullOrEmpty(SelectedImagePath);

        return new UniversalCommand
        {
            Id = _command.Id,
            Name = ActionName.Trim(),
            Type = (UniversalCommandType)ActionTypeIndex,
            KeyboardChord = RecordedChordInts,
            PowerShellCommand = PowerShellCommand,
            TargetAppProcessName = IsGlobal ? null : TargetAppProcessName.Trim(),
            IconGlyph = useImage ? '\0' : SelectedIconGlyph,
            IconImagePath = useImage ? SelectedImagePath : null,
        };
    }

    /// <summary>
    /// Discovers all glyphs in the Segoe Fluent Icons font by scanning the
    /// Unicode Private Use Area (E000–F8FF) and filtering out codepoints that
    /// render as the .notdef (missing) glyph.
    /// </summary>
    private static List<string> DiscoverGlyphs()
    {
        if (cachedGlyphs is not null)
        {
            return cachedGlyphs;
        }

        var glyphs = new List<string>(2048);

        using var device = CanvasDevice.GetSharedDevice();
        using var format = new CanvasTextFormat
        {
            FontFamily = "Segoe Fluent Icons",
            FontSize = 16
        };

        // Render a known-missing codepoint to get the .notdef glyph signature.
        byte[] notdefSignature = RenderGlyphToBytes(device, format, "\uE000");

        for (int codepoint = 0xE001; codepoint <= 0xF8FF; codepoint++)
        {
            string ch = char.ConvertFromUtf32(codepoint);
            byte[] rendered = RenderGlyphToBytes(device, format, ch);

            if (!rendered.AsSpan().SequenceEqual(notdefSignature))
            {
                glyphs.Add(ch);
            }
        }

        cachedGlyphs = glyphs;
        return glyphs;
    }

    private static byte[] RenderGlyphToBytes(CanvasDevice device, CanvasTextFormat format, string text)
    {
        const int Size = 20;

        using var renderTarget = new CanvasRenderTarget(device, Size, Size, 96);
        using (CanvasDrawingSession session = renderTarget.CreateDrawingSession())
        {
            session.Clear(Microsoft.UI.Colors.Transparent);
            session.DrawText(text, 0, 0, Microsoft.UI.Colors.White, format);
        }

        return renderTarget.GetPixelBytes();
    }

    private async Task LoadGlyphsAsync()
    {
        List<string> glyphs = await Task.Run(DiscoverGlyphs);
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            Glyphs = glyphs;

            string current = SelectedIconGlyph.ToString();
            int index = glyphs.IndexOf(current);
            SelectedGlyphIndex = index >= 0 ? index : -1;
            IsLoadingGlyphs = false;
        });
    }

    private void ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(ActionName))
        {
            _contentDialog.IsPrimaryButtonEnabled = false;
            return;
        }

        bool isShortcut = ActionTypeIndex == 0;
        if (isShortcut && RecordedChordInts.Count == 0)
        {
            _contentDialog.IsPrimaryButtonEnabled = false;
            return;
        }

        if (!isShortcut && string.IsNullOrWhiteSpace(PowerShellCommand))
        {
            _contentDialog.IsPrimaryButtonEnabled = false;
            return;
        }

        if (!IsGlobal && string.IsNullOrWhiteSpace(TargetAppProcessName))
        {
            _contentDialog.IsPrimaryButtonEnabled = false;
            return;
        }

        _contentDialog.IsPrimaryButtonEnabled = true;
    }

    partial void OnActionNameChanged(string value)
    {
        ValidateForm();
    }

    partial void OnActionTypeIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsShortcutType));
        OnPropertyChanged(nameof(IsPowerShellType));
        ValidateForm();
    }

    partial void OnIconTypeIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsGlyphMode));
        OnPropertyChanged(nameof(IsImageMode));
        UpdateIconPreview();
    }

    partial void OnSelectedIconGlyphChanged(char value)
    {
        UpdateIconPreview();
    }

    partial void OnSelectedImagePathChanged(string? value)
    {
        OnPropertyChanged(nameof(ImageFileName));
        UpdateIconPreview();
    }

    partial void OnPowerShellCommandChanged(string value)
    {
        ValidateForm();
    }

    partial void OnIsGlobalChanged(bool value)
    {
        OnPropertyChanged(nameof(IsTargetAppVisible));
        ValidateForm();
    }

    partial void OnTargetAppProcessNameChanged(string value)
    {
        ValidateForm();
    }

    partial void OnSelectedGlyphIndexChanged(int value)
    {
        if (Glyphs is not null && value >= 0 && value < Glyphs.Count && Glyphs[value].Length > 0)
        {
            SelectedIconGlyph = Glyphs[value][0];
        }
    }
}
