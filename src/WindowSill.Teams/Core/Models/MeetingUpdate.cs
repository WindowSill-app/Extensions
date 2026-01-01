
namespace WindowSill.Teams.Core.Models;

internal sealed class MeetingUpdate
{
    public MeetingState? MeetingState { get; set; }

    public MeetingPermissions? MeetingPermissions { get; set; }

    public override bool Equals(object? obj)
    {
        return obj is MeetingUpdate update &&
               EqualityComparer<MeetingState?>.Default.Equals(MeetingState, update.MeetingState) &&
               EqualityComparer<MeetingPermissions?>.Default.Equals(MeetingPermissions, update.MeetingPermissions);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(MeetingState, MeetingPermissions);
    }
}
