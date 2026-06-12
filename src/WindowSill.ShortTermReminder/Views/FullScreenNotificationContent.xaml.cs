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

        InitializeComponent();

        TrySetAudioSource();
    }

    /// <summary>
    /// Gets the view model for this notification content.
    /// </summary>
    internal FullScreenNotificationViewModel ViewModel { get; }

    private void TrySetAudioSource()
    {
        try
        {
            var uri = new Uri($@"{Environment.GetFolderPath(Environment.SpecialFolder.Windows)}\media\Windows Notify Calendar.wav");
            BackgroundMediaPlayer.Source = MediaSource.CreateFromUri(uri);
        }
        catch (Exception ex)
        {
            // Some Windows editions (e.g. "N"/"KN" without the Media Feature Pack) don't have the
            // media components registered, so MediaSource.CreateFromUri throws REGDB_E_CLASSNOTREG.
            // Audio is optional for the reminder, so degrade gracefully and still show the notification
            // without sound instead of letting the exception crash the app.
            this.Log().LogWarning(ex, "Unable to load reminder notification audio. The reminder will be shown without sound.");
        }
    }

    private void BackgroundMediaPlayer_Loaded(object sender, RoutedEventArgs e)
    {
        if (_playAudio && BackgroundMediaPlayer.Source is not null)
        {
            try
            {
                BackgroundMediaPlayer.MediaPlayer.Play();
            }
            catch (Exception ex)
            {
                this.Log().LogWarning(ex, "Unable to play reminder notification audio.");
            }
        }
    }
}
