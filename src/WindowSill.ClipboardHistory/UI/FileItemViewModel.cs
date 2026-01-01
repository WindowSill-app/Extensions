using System.Collections.ObjectModel;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.System;
using WindowSill.API;

namespace WindowSill.ClipboardHistory.UI;

internal sealed partial class FileItemViewModel : ClipboardHistoryItemViewModelBase
{
    private readonly ILogger _logger;
    private readonly SillListViewButtonItem _view;

    private FileItemViewModel(IProcessInteractionService processInteractionService, ClipboardHistoryItem item)
        : base(processInteractionService, item)
    {
        _logger = this.Log();
        _view = new SillListViewButtonItem(base.PasteCommand)
            .DataContext(
                this,
                (view, viewModel) => view
                    .PreviewFlyoutContent(
                        new StackPanel()
                            .Spacing(8)
                            .Padding(8)
                            .Children(
                                new TextBlock()
                                    .Style(x => x.ThemeResource("BodyTextBlockStyle"))
                                    .Text(x => x.Binding(() => viewModel.FileCountText))
                                    .HorizontalAlignment(HorizontalAlignment.Center),

                                new TextBlock()
                                    .Style(x => x.ThemeResource("CaptionTextBlockStyle"))
                                    .Foreground(x => x.ThemeResource("TextFillColorSecondaryBrush"))
                                    .Text(x => x.Binding(() => viewModel.FileListText))
                                    .TextWrapping(TextWrapping.Wrap)
                            )
                    )
                    .Content(
                        new Grid()
                            .ColumnSpacing(8)
                            .Margin(8, 0, 0, 0)
                            .ColumnDefinitions(
                                GridLength.Auto,
                                new GridLength(1, GridUnitType.Star),
                                GridLength.Auto
                            )
                            .Children(
                                new SymbolIcon()
                                    .Grid(column: 0)
                                    .Symbol(Symbol.Document)
                                    .MaxHeight(16)
                                    .MaxWidth(16),

                                new TextBlock()
                                    .Grid(column: 1)
                                    .VerticalAlignment(VerticalAlignment.Center)
                                    .TextTrimming(TextTrimming.CharacterEllipsis)
                                    .TextWrapping(TextWrapping.NoWrap)
                                    .Text(x => x.Binding(() => viewModel.DisplayText)),

                                new Button()
                                    .Grid(column: 2)
                                    .Style(x => x.StaticResource("IconButton"))
                                    .MaxWidth(24)
                                    .Content("\uE8A7")
                                    .ToolTipService(toolTip: "/WindowSill.ClipboardHistory/Misc/OpenInFileExplorer".GetLocalizedString())
                                    .Command(x => x.Binding(() => viewModel.OpenInFileExplorerCommand))
                            )
                    )
            );

        InitializeAsync().Forget();
    }

    internal static (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateView(IProcessInteractionService processInteractionService, ClipboardHistoryItem item)
    {
        var viewModel = new FileItemViewModel(processInteractionService, item);
        return (viewModel, viewModel._view);
    }

    [ObservableProperty]
    public partial string DisplayText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FileCountText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string FileListText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<FileInfo> Files { get; set; } = new();

    [RelayCommand]
    private async Task OpenInFileExplorerAsync()
    {
        if (Files.Count > 0)
        {
            FileInfo firstFile = Files[0];
            if (System.IO.File.Exists(firstFile.Path))
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(System.IO.Path.GetDirectoryName(firstFile.Path)!);
                await Launcher.LaunchFolderAsync(folder);
            }
            else if (System.IO.Directory.Exists(firstFile.Path))
            {
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(firstFile.Path);
                await Launcher.LaunchFolderAsync(folder);
            }
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            Guard.IsNotNull(Data);

            if (Data.AvailableFormats.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> storageItems = await Data.GetStorageItemsAsync();
                var fileInfos = new List<FileInfo>();

                foreach (IStorageItem? item in storageItems)
                {
                    if (item is StorageFile file)
                    {
                        var fileInfo = new FileInfo
                        {
                            Name = file.Name,
                            Path = file.Path,
                            Size = await GetFileSizeAsync(file)
                        };
                        fileInfos.Add(fileInfo);
                    }
                    else if (item is StorageFolder folder)
                    {
                        var folderInfo = new FileInfo
                        {
                            Name = folder.Name,
                            Path = folder.Path,
                            IsFolder = true
                        };
                        fileInfos.Add(folderInfo);
                    }
                }

                Files = new ObservableCollection<FileInfo>(fileInfos);

                if (Files.Count == 1)
                {
                    DisplayText = Files[0].Name;
                    FileCountText = Files[0].IsFolder ?
                        "/WindowSill.ClipboardHistory/Misc/OneFolder".GetLocalizedString() :
                        $"{Files[0].Name} ({Files[0].SizeText})";
                    FileListText = Files[0].Name;
                }
                else
                {
                    DisplayText = $"{Files.Count} " + "/WindowSill.ClipboardHistory/Misc/Items".GetLocalizedString();
                    int fileCount = Files.Count(f => !f.IsFolder);
                    int folderCount = Files.Count(f => f.IsFolder);

                    if (fileCount > 0 && folderCount > 0)
                    {
                        FileCountText = $"{fileCount} " + "/WindowSill.ClipboardHistory/Misc/Files".GetLocalizedString() +
                                       $", {folderCount} " + "/WindowSill.ClipboardHistory/Misc/Folders".GetLocalizedString();
                    }
                    else if (fileCount > 0)
                    {
                        FileCountText = $"{fileCount} " + "/WindowSill.ClipboardHistory/Misc/Files".GetLocalizedString();
                    }
                    else
                    {
                        FileCountText = $"{folderCount} " + "/WindowSill.ClipboardHistory/Misc/Folders".GetLocalizedString();
                    }

                    // Create a simple list of file names for preview
                    FileListText = string.Join("\n", Files.Take(10).Select(f => f.Name));
                    if (Files.Count > 10)
                    {
                        FileListText += $"\n... and {Files.Count - 10} more";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to initialize {nameof(FileItemViewModel)} control.");
            DisplayText = "/WindowSill.ClipboardHistory/Misc/FileError".GetLocalizedString();
        }
    }

    private static async Task<long> GetFileSizeAsync(StorageFile file)
    {
        try
        {
            BasicProperties properties = await file.GetBasicPropertiesAsync();
            return (long)properties.Size;
        }
        catch
        {
            return 0;
        }
    }

    public class FileInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public long Size { get; set; }
        public bool IsFolder { get; set; }

        public string SizeText => IsFolder ? string.Empty : FormatFileSize(Size);

        private static string FormatFileSize(long bytes)
        {
            if (bytes == 0)
            {
                return "0 B";
            }

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }
}
