using WindowSill.ClipboardHistory.ViewModels;
using WindowSill.API;

namespace WindowSill.ClipboardHistory.Views;

/// <summary>
/// Content view for URI clipboard items, displayed in the list row.
/// </summary>
internal sealed partial class UriItemContentView : UserControl
{
    internal UriItemContentView(UriItemViewModel viewModel)
    {
        ViewModel = viewModel;
        OpenInBrowserTooltip = "/WindowSill.ClipboardHistory/Misc/OpenInWebBrowser".GetLocalizedString();
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this view.
    /// </summary>
    internal UriItemViewModel ViewModel { get; }

    /// <summary>
    /// Gets the localized tooltip for the open in browser button.
    /// </summary>
    internal string OpenInBrowserTooltip { get; }
}
