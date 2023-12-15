using Discord;
using Zeenox.Models.Player;
using Zeenox.Models.Socket;
using ActionType = Zeenox.Enums.ActionType;

namespace Zeenox.Models.Actions.Player;

public class SkipAction(IUser user, ExtendedTrackItem previous, ExtendedTrackItem trackItem) : Action(user, ActionType.Skip)
{
    public TrackDto PreviousTrack { get; } = new(previous);
    public TrackDto Track { get; } = new(trackItem);
    
    public override string Stringify()
    {
        return $"skipped to: {trackItem.Title}";
    }
}