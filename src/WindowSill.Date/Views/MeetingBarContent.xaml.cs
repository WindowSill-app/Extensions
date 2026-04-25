using Windows.System;

using WindowSill.API;
using WindowSill.Date.ViewModels;

namespace WindowSill.Date.Views;

/// <summary>
/// Bar content for a single meeting sill item.
/// Shows title + countdown in single line (Medium/Small) or two lines (Large).
/// Includes an optional Join button for video call meetings.
/// </summary>
internal sealed partial class MeetingBarContent : UserControl
{
    private MeetingBarContent(MeetingSillItemViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model for data binding.
    /// </summary>
    internal MeetingSillItemViewModel ViewModel { get; }

    /// <summary>
    /// Creates a <see cref="MeetingBarContent"/> instance for use as sill item content.
    /// Wires orientation change handling.
    /// </summary>
    /// <param name="viewModel">The meeting view model.</param>
    /// <returns>The bar content UserControl.</returns>
    internal static MeetingBarContent Create(MeetingSillItemViewModel viewModel)
    {
        return new MeetingBarContent(viewModel);
    }

    private void JoinButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.VideoCallUrl is not null)
        {
            Launcher.LaunchUriAsync(ViewModel.VideoCallUrl).AsTask().ForgetSafely();
        }
    }
}
