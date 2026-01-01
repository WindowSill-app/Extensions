using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using WindowSill.API;

namespace WindowSill.ClipboardHistory.UI;

internal sealed partial class ApplicationLinkItemViewModel : ClipboardHistoryItemViewModelBase
{
    private readonly ILogger _logger;
    private readonly SillListViewButtonItem _view;

    private ApplicationLinkItemViewModel(IProcessInteractionService processInteractionService, ClipboardHistoryItem item)
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
                                        new FontIcon()
                                            .Grid(column: 0)
                                            .FontSize(24)
                                            .Glyph("\uE8AD"), // App icon

                                        new TextBlock()
                                            .Grid(column: 1)
                                            .Style(x => x.ThemeResource("BodyTextBlockStyle"))
                                            .TextTrimming(TextTrimming.CharacterEllipsis)
                                            .TextWrapping(TextWrapping.NoWrap)
                                            .VerticalAlignment(VerticalAlignment.Center)
                                            .Text(x => x.Binding(() => viewModel.DisplayText))
                                    ),

                                new TextBlock()
                                    .Style(x => x.ThemeResource("CaptionTextBlockStyle"))
                                    .Foreground(x => x.ThemeResource("TextFillColorSecondaryBrush"))
                                    .Text(x => x.Binding(() => viewModel.ApplicationLink))
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
                                new FontIcon()
                                    .Grid(column: 0)
                                    .FontSize(16)
                                    .Glyph("\uE8AD"), // App icon

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
                                    .ToolTipService(toolTip: "/WindowSill.ClipboardHistory/Misc/OpenApplication".GetLocalizedString())
                                    .Command(() => viewModel.OpenApplicationCommand)
                            )
                    )
            );

        InitializeAsync().Forget();
    }

    internal static (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateView(IProcessInteractionService processInteractionService, ClipboardHistoryItem item)
    {
        var viewModel = new ApplicationLinkItemViewModel(processInteractionService, item);
        return (viewModel, viewModel._view);
    }

    [ObservableProperty]
    public partial string ApplicationLink { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DisplayText { get; set; } = string.Empty;

    [RelayCommand]
    private async Task OpenApplicationAsync()
    {
        if (Uri.TryCreate(ApplicationLink, UriKind.Absolute, out Uri? uri))
        {
            await Launcher.LaunchUriAsync(uri);
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            Guard.IsNotNull(Data);

            // Get the application link data
            Uri appLink = await Data.GetApplicationLinkAsync();
            ApplicationLink = appLink?.ToString() ?? string.Empty;
            DisplayText = ApplicationLink;

            // Try to get a better display text from plain text if available
            if (Data.Contains(StandardDataFormats.Text))
            {
                string text = await Data.GetTextAsync();
                if (!string.IsNullOrEmpty(text) && text != ApplicationLink)
                {
                    DisplayText = text;
                }
            }

            // Fallback display text
            if (string.IsNullOrEmpty(DisplayText))
            {
                DisplayText = "Application Link";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to initialize {nameof(ApplicationLinkItemViewModel)} control.");
            ApplicationLink = string.Empty;
            DisplayText = "Application Link";
        }
    }
}
