using Windows.Media.Core;

using WindowSill.Date.ViewModels;

namespace WindowSill.Date.Views;

/// <summary>
/// UserControl providing the visual content for a full-screen departure notification.
/// Shows location, travel time, and "Open in Maps" action.
/// </summary>
internal sealed partial class DepartureNotificationContent : UserControl
{
    private readonly bool _playAudio;

    /// <summary>
    /// Initializes a new instance of the <see cref="DepartureNotificationContent"/> class.
    /// </summary>
    /// <param name="viewModel">The departure notification view model.</param>
    /// <param name="playAudio">Whether to play the notification sound.</param>
    internal DepartureNotificationContent(DepartureNotificationViewModel viewModel, bool playAudio)
    {
        ViewModel = viewModel;
        _playAudio = playAudio;

        var uri = new Uri($@"{Environment.GetFolderPath(Environment.SpecialFolder.Windows)}\media\Windows Notify Calendar.wav");
        var audioSource = MediaSource.CreateFromUri(uri);

        InitializeComponent();

        BackgroundMediaPlayer.Source = audioSource;
    }

    /// <summary>
    /// Gets the view model for this notification content.
    /// </summary>
    internal DepartureNotificationViewModel ViewModel { get; }

    private void BackgroundMediaPlayer_Loaded(object sender, RoutedEventArgs e)
    {
        if (_playAudio)
        {
            BackgroundMediaPlayer.MediaPlayer.Play();
        }
    }
}
