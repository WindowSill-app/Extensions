using CommunityToolkit.Mvvm.ComponentModel;
using WindowSill.API;
using WindowSill.ClipboardHistory.Core;

namespace WindowSill.ClipboardHistory.ViewModels;

internal sealed partial class UnknownItemViewModel : ClipboardHistoryItemViewModelBase
{
    /// <summary>
    /// Gets or sets the truncated display text shown in the list item.
    /// </summary>
    [ObservableProperty]
    public partial string DisplayText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full text shown in the preview flyout.
    /// </summary>
    [ObservableProperty]
    public partial string PreviewText { get; set; } = string.Empty;

    internal UnknownItemViewModel(IProcessInteractionService processInteractionService, IClipboardItemSource source)
        : base(processInteractionService, source)
    {
        DisplayText = "/WindowSill.ClipboardHistory/Misc/UnsupportedFormat".GetLocalizedString();
        PreviewText = string.Join(", ", Data.AvailableFormats);
    }
}
