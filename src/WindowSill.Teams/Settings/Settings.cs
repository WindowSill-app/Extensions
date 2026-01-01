using WindowSill.API;

namespace WindowSill.Teams.Settings;

internal static class Settings
{
    /// <summary>
    /// A token provided by Microsoft Teams to keep for later use.
    /// </summary>
    internal static readonly SettingDefinition<string> Token
        = new(string.Empty, typeof(Settings).Assembly);
}
