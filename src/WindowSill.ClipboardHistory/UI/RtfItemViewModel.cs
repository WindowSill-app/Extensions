using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.DataTransfer;
using WindowSill.API;

namespace WindowSill.ClipboardHistory.UI;

internal sealed class RtfItemViewModel : ClipboardHistoryItemViewModelBase
{
    private readonly ILogger _logger;
    private readonly SillListViewButtonItem _view;

    private RtfItemViewModel(IProcessInteractionService processInteractionService, ClipboardHistoryItem item)
        : base(processInteractionService, item)
    {
        _logger = this.Log();
        _view = new SillListViewButtonItem(base.PasteCommand);
        _view.DataContext = this;

        InitializeAsync().Forget();
    }

    internal static (ClipboardHistoryItemViewModelBase, SillListViewItem) CreateView(IProcessInteractionService processInteractionService, ClipboardHistoryItem item)
    {
        var viewModel = new RtfItemViewModel(processInteractionService, item);
        return (viewModel, viewModel._view);
    }

    private async Task InitializeAsync()
    {
        try
        {
            Guard.IsNotNull(Data);

            // Try to use plain text rather than RTF if available
            string displayText;
            if (Data.Contains(StandardDataFormats.Text))
            {
                displayText = await Data.GetTextAsync();
            }
            else
            {
                displayText = await Data.GetRtfAsync();
            }

            _view.Content = displayText
                .Substring(0, Math.Min(displayText.Length, 256))
                .Trim()
                .Replace("\r\n", "⏎")
                .Replace("\n\r", "⏎")
                .Replace('\r', '⏎')
                .Replace('\n', '⏎');

            _view.PreviewFlyoutContent = displayText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to initialize {nameof(RtfItemViewModel)} control.");
        }
    }
}
