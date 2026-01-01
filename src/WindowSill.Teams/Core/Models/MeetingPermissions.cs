namespace WindowSill.Teams.Core.Models;

/// <param name="CanToggleMute"> Allowed to mute/unmute </param>
/// <param name="CanToggleVideo"> Allowed to toggle video </param>
/// <param name="CanToggleHand"> Allowed to Raise hand </param>
/// <param name="CanToggleBlur"> Allowed to toggle blur </param>
/// <param name="CanLeave"> Allowed to leave </param>
/// <param name="CanReact"> Allowed to react in current call </param>
/// <param name="CanToggleShareTray"> Allowed to toggle share tray </param>
/// <param name="CanToggleChat"> Allowed to toggle chat </param>
/// <param name="CanStopSharing"> Allowed to stop sharing </param>
/// <param name="CanPair"> Allowed to pair </param>
internal sealed record MeetingPermissions(
    bool CanToggleMute,
    bool CanToggleVideo,
    bool CanToggleHand,
    bool CanToggleBlur,
    bool CanLeave,
    bool CanReact,
    bool CanToggleShareTray,
    bool CanToggleChat,
    bool CanStopSharing,
    bool CanPair);
