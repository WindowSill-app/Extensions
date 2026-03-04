using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using WindowSill.API;
using WindowSill.TextFinder.Core;
using WindowSill.TextFinder.Views;
using Path = System.IO.Path;

namespace WindowSill.WebBrowser;

[Export(typeof(ISill))]
[Name("Text Finder")]
public sealed partial class TextFinderSill : ObservableObject, ISillActivatedByDragAndDrop, ISillActivatedByTextSelection, ISillListView
{
    private readonly IPluginInfo _pluginInfo;
    private InputData? _inputData;

    [ImportingConstructor]
    public TextFinderSill(IPluginInfo pluginInfo)
    {
        _pluginInfo = pluginInfo;
    }

    public string DisplayName => "/WindowSill.TextFinder/Misc/DisplayName".GetLocalizedString();

    public IconElement CreateIcon()
        => new ImageIcon
        {
            Source = new SvgImageSource(new Uri(System.IO.Path.Combine(_pluginInfo.GetPluginContentDirectory(), "Assets", "search.svg")))
        };

    public ObservableCollection<SillListViewItem> ViewList { get; } = [];

    public SillView? PlaceholderView => null;

    public SillSettingsView[]? SettingsViews => throw new NotImplementedException();

    public string[] TextSelectionActivatorTypeNames => [PredefinedActivationTypeNames.TextSelection];

    public string[] DragAndDropActivatorTypeNames
        =>
        [
            PredefinedActivationTypeNames.PlainTextFileDrop,
            PredefinedActivationTypeNames.PdfFileDrop,
            PredefinedActivationTypeNames.DocxFileDrop
        ];

    public async ValueTask OnActivatedAsync(string textSelectionActivatorTypeName, WindowTextSelection currentSelection)
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            UpdateViewList(currentSelection);
        });
    }

    public async ValueTask OnActivatedAsync(string dragAndDropActivatorTypeName, DataPackageView data)
    {
        try
        {
            string filePath = string.Empty;
            if (data.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> storageItems = await data.GetStorageItemsAsync();
                if (storageItems.Count == 1 && storageItems[0] is IStorageFile storageFile)
                {
                    filePath = storageFile.Path;
                }
            }

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return;
            }

            await ThreadHelper.RunOnUIThreadAsync(() =>
            {
                UpdateViewList(dragAndDropActivatorTypeName, filePath);
            });
        }
        catch
        {
            await OnDeactivatedAsync();
        }
    }

    public async ValueTask OnDeactivatedAsync()
    {
        await ThreadHelper.RunOnUIThreadAsync(() =>
        {
            _inputData = null;
            ViewList.Clear();
        });
    }

    private void UpdateViewList(string dragAndDropActivatorTypeName, string filePath)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        _inputData = new InputData
        {
            DragAndDropActivatorTypeName = dragAndDropActivatorTypeName,
            FilePath = filePath
        };

        ViewList.Clear();
        ViewList.Add(
            new SillListViewButtonItem(
                text: string.Format("/WindowSill.TextFinder/Misc/SillTitleSelectedFile".GetLocalizedString(), Path.GetFileName(filePath)),
                null,
                FindCommand));
    }

    private void UpdateViewList(WindowTextSelection currentSelection)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        _inputData = new InputData
        {
            TextSelection = currentSelection
        };

        ViewList.Clear();
        ViewList.Add(
            new SillListViewButtonItem(
                text: "/WindowSill.TextFinder/Misc/SillTitleSelectedText".GetLocalizedString(),
                null,
                FindCommand));
    }

    [RelayCommand]
    private async Task FindAsync()
    {
        SillListViewItem[] viewList = ViewList.ToArray();
        if (viewList.Length == 0)
        {
            return;
        }

        InputData? inputData = _inputData;
        if (inputData is null)
        {
            return;
        }

        var findWindow = new FindWindow(_pluginInfo, inputData);
        findWindow.ShowAndCenter(viewList[0], 1000, 600);
    }
}
