using WindowSill.ClipboardHistory.ViewModels;
using WindowSill.API;

namespace WindowSill.ClipboardHistory.Views;

/// <summary>
/// Content view for application link clipboard items, displayed in the list row.
/// </summary>
internal sealed partial class ApplicationLinkItemContentView : UserControl
{
    internal ApplicationLinkItemContentView(ApplicationLinkItemViewModel viewModel)
    {
        ViewModel = viewModel;
        OpenApplicationTooltip = "/WindowSill.ClipboardHistory/Misc/OpenApplication".GetLocalizedString();
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for this view.
    /// </summary>
    internal ApplicationLinkItemViewModel ViewModel { get; }

    /// <summary>
    /// Gets the localized tooltip for the open application button.
    /// </summary>
    internal string OpenApplicationTooltip { get; }
}
