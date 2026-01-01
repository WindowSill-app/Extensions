using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using WindowSill.API;

namespace WindowSill.ClipboardHistory.UI;

internal abstract partial class ClipboardHistoryItemViewModelBase
    : ObservableObject,
    IEquatable<ClipboardHistoryItemViewModelBase>,
    IEquatable<ClipboardHistoryItem>
{
    private readonly IProcessInteractionService _processInteractionService;
    private readonly ClipboardHistoryItem _item;

    protected ClipboardHistoryItemViewModelBase(
        IProcessInteractionService processInteractionService,
        ClipboardHistoryItem item)
        : base()
    {
        Guard.IsNotNull(processInteractionService);
        Guard.IsNotNull(item);
        _processInteractionService = processInteractionService;
        _item = item;
        Data = item.Content;
    }

    public DataPackageView Data { get; }

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
            Clipboard.SetHistoryItemAsContent(_item);

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
