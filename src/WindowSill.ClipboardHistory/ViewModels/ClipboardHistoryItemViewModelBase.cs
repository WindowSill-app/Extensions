using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using WindowSill.API;
using WindowSill.ClipboardHistory.Core;

namespace WindowSill.ClipboardHistory.ViewModels;

public abstract partial class ClipboardHistoryItemViewModelBase
    : ObservableObject,
    IEquatable<ClipboardHistoryItemViewModelBase>,
    IEquatable<ClipboardHistoryItem>
{
    private readonly ILogger _logger;
    private readonly IProcessInteractionService _processInteractionService;
    private readonly ClipboardHistoryItem _item;

    protected ClipboardHistoryItemViewModelBase(
        IProcessInteractionService processInteractionService,
        ClipboardHistoryItem item)
        : base()
    {
        Guard.IsNotNull(processInteractionService);
        Guard.IsNotNull(item);
        _logger = this.Log();
        _processInteractionService = processInteractionService;
        _item = item;
        Data = item.Content;
    }

    public DataPackageView Data { get; }

    /// <summary>
    /// Gets the detected data type for this clipboard item.
    /// Subclasses should override this to return the correct type for paste-as-file support.
    /// </summary>
    protected virtual DetectedClipboardDataType DetectedDataType => DetectedClipboardDataType.Unknown;

    public bool Equals(ClipboardHistoryItemViewModelBase? other)
    {
        return other is not null && Equals(other._item);
    }

    public bool Equals(ClipboardHistoryItem? other)
    {
        return other is not null && string.Equals(_item.Id, other.Id, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ClipboardHistoryItemViewModelBase)
            || Equals(obj as ClipboardHistoryItem);
    }

    public override int GetHashCode()
    {
        return _item.Id.GetHashCode();
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
            Clipboard.SetHistoryItemAsContent(_item);

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
            Clipboard.DeleteItemFromHistory(_item);
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
}
