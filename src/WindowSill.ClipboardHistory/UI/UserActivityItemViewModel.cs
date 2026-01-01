using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;
using WindowSill.API;

namespace WindowSill.ClipboardHistory.UI;

internal sealed partial class UserActivityItemViewModel : ClipboardHistoryItemViewModelBase
{
    private readonly ILogger _logger;
    private readonly SillListViewButtonItem _view;

    private UserActivityItemViewModel(IProcessInteractionService processInteractionService, ClipboardHistoryItem item)
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
                                            .Glyph("\uE81C"), // Timeline/Activity icon

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
                                    .Text("User Activity Data")
                            )
                    )
                    .Content(
                        new Grid()
                            .ColumnSpacing(8)
                            .Margin(8, 0, 0, 0)
                            .ColumnDefinitions(
                                GridLength.Auto,
                                new GridLength(1, GridUnitType.Star)
                            )
                            .Children(
                                new FontIcon()
                                    .Grid(column: 0)
                                    .FontSize(16)
                                    .Glyph("\uE81C"), // Timeline/Activity icon

                                new TextBlock()
                                    .Grid(column: 1)
                                    .VerticalAlignment(VerticalAlignment.Center)
                                    .TextTrimming(TextTrimming.CharacterEllipsis)
                                    .TextWrapping(TextWrapping.NoWrap)
                                    .Text(x => x.Binding(() => viewModel.DisplayText))
                            )
                    )
            );

        InitializeAsync().Forget();
    }

    internal static (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateView(IProcessInteractionService processInteractionService, ClipboardHistoryItem item)
    {
        var viewModel = new UserActivityItemViewModel(processInteractionService, item);
        return (viewModel, viewModel._view);
    }

    [ObservableProperty]
    public partial string DisplayText { get; set; } = string.Empty;

    private async Task InitializeAsync()
    {
        try
        {
            Guard.IsNotNull(Data);

            // Get the user activity JSON data
            string userActivityJson = await Data.GetDataAsync(StandardDataFormats.UserActivityJsonArray) as string ?? string.Empty;

            // For display, show a truncated version of the JSON or extract meaningful info
            DisplayText = string.IsNullOrEmpty(userActivityJson)
                ? "User Activity Data"
                : userActivityJson.Substring(0, Math.Min(userActivityJson.Length, 100)).Trim();

            // Clean up display text for better readability
            if (!string.IsNullOrEmpty(DisplayText))
            {
                DisplayText = DisplayText
                    .Replace("\r\n", " ")
                    .Replace("\n\r", " ")
                    .Replace('\r', ' ')
                    .Replace('\n', ' ')
                    .Replace("  ", " ")
                    .Trim();

                if (DisplayText.Length > 50)
                {
                    DisplayText = DisplayText.Substring(0, 50) + "...";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to initialize {nameof(UserActivityItemViewModel)} control.");
            DisplayText = "User Activity Data";
        }
    }
}
