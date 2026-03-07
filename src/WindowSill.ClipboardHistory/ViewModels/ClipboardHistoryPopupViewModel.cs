using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.ApplicationModel.DataTransfer;
using WindowSill.API;

namespace WindowSill.ClipboardHistory.ViewModels;

/// <summary>
/// ViewModel for the compact mode clipboard history popup.
/// Manages the collection of clipboard items displayed within the popup.
/// </summary>
internal sealed partial class ClipboardHistoryPopupViewModel : ObservableObject
{
    public ClipboardHistoryPopupViewModel()
    {
        Items.CollectionChanged += Items_CollectionChanged;
    }

    /// <summary>
    /// Gets the collection of clipboard history item ViewModels displayed in the popup.
    /// </summary>
    public ObservableCollection<ClipboardHistoryItemViewModelBase> Items { get; } = [];

    public Visibility EmptyClipboardIndicatorVisibility => Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Gets the command to clear all clipboard history.
    /// </summary>
    [RelayCommand]
    private async Task ClearAllAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            Clipboard.ClearHistory();
        });
    }

    private void Items_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(EmptyClipboardIndicatorVisibility));
    }
}
