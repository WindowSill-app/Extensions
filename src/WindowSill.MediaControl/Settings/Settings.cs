using WindowSill.API;

namespace WindowSill.MediaControl.Settings;

internal static class Settings
{
    /// <summary>
    /// Whether to show the song title, artist and thumbnail or not.
    /// </summary>
    internal static readonly SettingDefinition<bool> ShowTitleArtistAndThumbnail
        = new(true, typeof(Settings).Assembly);
}
