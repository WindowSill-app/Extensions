using Windows.Media.Core;

using WindowSill.ShortTermReminder.ViewModels;

namespace WindowSill.ShortTermReminder.Views;

/// <summary>
/// UserControl providing the visual content for a full-screen reminder notification,
/// including the reminder title, dismiss and snooze actions, and audio playback.
/// </summary>
internal sealed partial class FullScreenNotificationContent : UserControl
{
    private readonly bool _playAudio;

    internal FullScreenNotificationContent(FullScreenNotificationViewModel viewModel, bool playAudio)
    {
        ViewModel = viewModel;
        _playAudio = playAudio;

        var uri = new Uri($@"{Environment.GetFolderPath(Environment.SpecialFolder.Windows)}\media\Windows Notify Calendar.wav");
        var audioNotificationMediaSource = MediaSource.CreateFromUri(uri);

        InitializeComponent();

        BackgroundMediaPlayer.Source = audioNotificationMediaSource;
    }

    /// <summary>
    /// Gets the view model for this notification content.
    /// </summary>
    internal FullScreenNotificationViewModel ViewModel { get; }

    private void BackgroundMediaPlayer_Loaded(object sender, RoutedEventArgs e)
    {
        if (_playAudio)
        {
            BackgroundMediaPlayer.MediaPlayer.Play();
        }
    }
}
