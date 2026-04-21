using WindowSill.Date.ViewModels;

namespace WindowSill.Date.Views;

/// <summary>
/// Hover preview flyout for a meeting sill item.
/// Shows full title, countdown, and exact date/time.
/// </summary>
internal sealed partial class MeetingPreviewFlyout : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MeetingPreviewFlyout"/> class.
    /// </summary>
    /// <param name="viewModel">The meeting view model.</param>
    internal MeetingPreviewFlyout(MeetingSillItemViewModel viewModel)
    {
        InitializeComponent();

        TitleText.Text = viewModel.Title;
        CountdownText.Text = viewModel.CountdownText;
        DateTimeText.Text = viewModel.FullDateTimeText;

        // Keep countdown text live.
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MeetingSillItemViewModel.CountdownText))
            {
                CountdownText.Text = viewModel.CountdownText;
            }
        };
    }
}
