using Windows.Media.Core;

using WindowSill.Date.ViewModels;

namespace WindowSill.Date.Views;

/// <summary>
/// UserControl providing the visual content for a full-screen meeting notification,
/// including the meeting title, time, join and dismiss actions, and audio playback.
/// </summary>
internal sealed partial class MeetingNotificationContent : UserControl
{
    private readonly bool _playAudio;

    /// <summary>
    /// Initializes a new instance of the <see cref="MeetingNotificationContent"/> class.
    /// </summary>
    /// <param name="viewModel">The notification view model.</param>
    /// <param name="playAudio">Whether to play the notification sound.</param>
    internal MeetingNotificationContent(MeetingNotificationViewModel viewModel, bool playAudio)
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
    internal MeetingNotificationViewModel ViewModel { get; }

    private void BackgroundMediaPlayer_Loaded(object sender, RoutedEventArgs e)
    {
        if (_playAudio)
        {
            BackgroundMediaPlayer.MediaPlayer.Play();
        }
    }
}
