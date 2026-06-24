using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using WindowSill.API;
using WindowSill.ClipboardHistory.Core;
using WindowSill.ClipboardHistory.Services;

namespace WindowSill.ClipboardHistory.ViewModels;

public abstract partial class ClipboardHistoryItemViewModelBase
    : ObservableObject,
    IEquatable<ClipboardHistoryItemViewModelBase>
{
    private readonly ILogger _logger;
    private readonly IProcessInteractionService _processInteractionService;
    private readonly IClipboardItemSource _source;

    private PinnedClipboardService? _pinnedService;
    private ClipboardItemData? _itemData;

    internal ClipboardHistoryItemViewModelBase(
        IProcessInteractionService processInteractionService,
        IClipboardItemSource source)
        : base()
    {
        Guard.IsNotNull(processInteractionService);
        Guard.IsNotNull(source);
        _logger = this.Log();
        _processInteractionService = processInteractionService;
        _source = source;
        Data = source.Data;
    }

    public DataPackageView Data { get; }

    /// <summary>
    /// Gets whether the item this view model represents is pinned.
    /// </summary>
    public bool IsPinned => _source.IsPinned;

    /// <summary>
    /// Gets whether another item can still be pinned (only meaningful for unpinned items).
    /// </summary>
    public bool CanPin => !IsPinned && _pinnedService is not null;

    /// <summary>
    /// Gets the stable identifier of the underlying clipboard item source.
    /// </summary>
    internal string SourceId => _source.Id;

    /// <summary>
    /// Wires up the pinning commands for this view model.
    /// </summary>
    internal void ConfigurePinning(ClipboardItemData itemData, PinnedClipboardService pinnedService)
    {
        _itemData = itemData;
        _pinnedService = pinnedService;
        OnPropertyChanged(nameof(CanPin));
        OnPropertyChanged(nameof(CanTogglePin));
    }

    /// <summary>
    /// Gets whether the pin/unpin toggle should be enabled (always allowed for pinned
    /// items, otherwise only when the pin limit has not been reached).
    /// </summary>
    public bool CanTogglePin => _pinnedService is not null;

    /// <summary>
    /// Gets the glyph for the pin/unpin toggle button (Segoe Fluent Icons).
    /// </summary>
    public string PinGlyph => IsPinned ? "\uE77A" : "\uE718";

    /// <summary>
    /// Gets the tooltip for the pin/unpin toggle button.
    /// </summary>
    public string PinTooltip => IsPinned
        ? "/WindowSill.ClipboardHistory/Misc/Unpin".GetLocalizedString()
        : "/WindowSill.ClipboardHistory/Misc/Pin".GetLocalizedString();

    /// <summary>
    /// Gets the detected data type for this clipboard item.
    /// Subclasses should override this to return the correct type for paste-as-file support.
    /// </summary>
    protected virtual DetectedClipboardDataType DetectedDataType => DetectedClipboardDataType.Unknown;

    public bool Equals(ClipboardHistoryItemViewModelBase? other)
    {
        return other is not null && string.Equals(_source.Id, other._source.Id, StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines whether this view model represents the given clipboard item source.
    /// </summary>
    internal bool Equals(IClipboardItemSource? other)
    {
        return other is not null && string.Equals(_source.Id, other.Id, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ClipboardHistoryItemViewModelBase);
    }

    public override int GetHashCode()
    {
        return _source.Id.GetHashCode();
    }

    [RelayCommand]
    private async Task PasteAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(async () =>
        {
            // Check if the last active window is Explorer/Desktop/FileDialog
            nint targetHwnd = ExplorerDetector.GetLastActiveWindow();
            if (targetHwnd != 0 && ExplorerDetector.IsExplorerLikeWindow(targetHwnd))
            {
                string? folderPath = ExplorerFolderResolver.GetCurrentFolderPath(targetHwnd);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    bool written = await ClipboardFileWriter.TryWriteAsFileAsync(Data, DetectedDataType, folderPath);
                    if (written)
                    {
                        _logger.LogInformation("Pasted clipboard content as file in {Folder}.", folderPath);
                        return;
                    }
                }
            }

            // Fall back to normal paste via Ctrl+V
            _source.SetAsClipboardContent();

            await Task.Delay(200);

            await _processInteractionService.SimulateKeysOnLastActiveWindow(
                VirtualKey.LeftControl,
                VirtualKey.V);
        });
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            _source.DeleteFromHistory();
        });
    }

    [RelayCommand]
    private async Task ClearAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            Clipboard.ClearHistory();
        });
    }

    [RelayCommand]
    private async Task PinAsync()
    {
        if (_pinnedService is null || _itemData is null)
        {
            return;
        }

        await Task.Run(() => _pinnedService.PinAsync(_itemData));
    }

    [RelayCommand]
    private async Task UnpinAsync()
    {
        if (_pinnedService is null)
        {
            return;
        }

        await Task.Run(() => _pinnedService.UnpinAsync(SourceId));
    }

    [RelayCommand]
    private async Task TogglePinAsync()
    {
        if (IsPinned)
        {
            await UnpinAsync();
        }
        else
        {
            await PinAsync();
        }
    }
}
