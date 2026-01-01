using System.Text.RegularExpressions;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using WindowSill.API;
using WindowSill.ClipboardHistory.Utils;

namespace WindowSill.ClipboardHistory.UI;

internal sealed partial class UriItemViewModel : ClipboardHistoryItemViewModelBase
{
    private readonly ILogger _logger;
    private readonly SillListViewButtonItem _view;

    private UriItemViewModel(IProcessInteractionService processInteractionService, ClipboardHistoryItem item)
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
                                new Grid()
                                    .ColumnSpacing(8)
                                    .HorizontalAlignment(HorizontalAlignment.Center)
                                    .ColumnDefinitions(
                                        GridLength.Auto,
                                        new GridLength(1, GridUnitType.Star)
                                    )
                                    .Children(
                                        new ImageIcon()
                                            .Grid(column: 0)
                                            .MaxHeight(24)
                                            .MaxWidth(24)
                                            .Source(x => x.Binding(() => viewModel.LargerFavicon)),

                                        new TextBlock()
                                            .Grid(column: 1)
                                            .Style(x => x.ThemeResource("BodyTextBlockStyle"))
                                            .TextTrimming(TextTrimming.CharacterEllipsis)
                                            .TextWrapping(TextWrapping.NoWrap)
                                            .VerticalAlignment(VerticalAlignment.Center)
                                            .Text(x => x.Binding(() => viewModel.PageTitle))
                                    ),

                                new TextBlock()
                                    .Style(x => x.ThemeResource("CaptionTextBlockStyle"))
                                    .Foreground(x => x.ThemeResource("TextFillColorSecondaryBrush"))
                                    .Text(x => x.Binding(() => viewModel.Url))
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
                                new ImageIcon()
                                    .Grid(column: 0)
                                    .MaxHeight(16)
                                    .MaxWidth(16)
                                    .Source(x => x.Binding(() => viewModel.Favicon)),

                                new TextBlock()
                                    .Grid(column: 1)
                                    .VerticalAlignment(VerticalAlignment.Center)
                                    .TextTrimming(TextTrimming.CharacterEllipsis)
                                    .TextWrapping(TextWrapping.NoWrap)
                                    .Text(x => x.Binding(() => viewModel.Url)),

                                new Button()
                                    .Grid(column: 2)
                                    .Style(x => x.StaticResource("IconButton"))
                                    .MaxWidth(24)
                                    .Content("\uE8A7")
                                    .ToolTipService(toolTip: "/WindowSill.ClipboardHistory/Misc/OpenInWebBrowser".GetLocalizedString())
                                    .Command(() => viewModel.OpenInBrowserCommand)
                            )
                    )
            );

        InitializeAsync().Forget();
    }

    internal static (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateView(IProcessInteractionService processInteractionService, ClipboardHistoryItem item)
    {
        var viewModel = new UriItemViewModel(processInteractionService, item);
        return (viewModel, viewModel._view);
    }

    [ObservableProperty]
    public partial string Url { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PageTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial BitmapImage? Favicon { get; set; }

    [ObservableProperty]
    public partial BitmapImage? LargerFavicon { get; set; }

    [RelayCommand]
    private async Task OpenInBrowserAsync()
    {
        await Launcher.LaunchUriAsync(new Uri(Url));
    }

    private async Task InitializeAsync()
    {
        try
        {
            Guard.IsNotNull(Data);

            if (Data.AvailableFormats.Contains(StandardDataFormats.WebLink))
            {
                Uri webLink = await Data.GetWebLinkAsync();
                Url = webLink?.ToString() ?? string.Empty;
            }
            else if (Data.AvailableFormats.Contains(StandardDataFormats.Uri))
            {
                Uri uri = await Data.GetUriAsync();
                Url = uri?.ToString() ?? string.Empty;
            }
            else
            {
                Url = await Data.GetTextAsync();
            }

            Favicon = new BitmapImage(DataHelper.GetFaviconGoogleUri(Url, 16));
            LargerFavicon = new BitmapImage(DataHelper.GetFaviconGoogleUri(Url, 24));

            if (Data.Contains(StandardDataFormats.Html))
            {
                string html = await Data.GetHtmlFormatAsync();
                if (!string.IsNullOrEmpty(html))
                {
                    // Extract the HTML part
                    int startHtml = html.IndexOf("<html>");
                    if (startHtml >= 0)
                    {
                        string htmlContent = html.Substring(startHtml);

                        // Use regex to find the text inside the <a> tag
                        Match match = AHrefTagRegex().Match(htmlContent);
                        if (match.Success)
                        {
                            string pageTitle = match.Groups[1].Value.Trim();
                            PageTitle = pageTitle;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to initialize {nameof(UriItemViewModel)} control.");
        }
    }

    [GeneratedRegex(@"<a[^>]*>(.*?)<\/a>", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex AHrefTagRegex();
}
