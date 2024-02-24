using Discord;
using Lavalink4NET.Players.Queued;
using ActionType = Zeenox.Enums.ActionType;

namespace Zeenox.Models.Actions.Player;

public class RepeatAction(IUser user, TrackRepeatMode loopMode) : Action(user, ActionType.ChangeLoopMode)
{
    public TrackRepeatMode TrackRepeatMode { get; set; } = loopMode;

    public override string Stringify()
    {
        return TrackRepeatMode switch
        {
            TrackRepeatMode.None => "disabled loop mode",
            TrackRepeatMode.Track => "set current track to be repeated",
            TrackRepeatMode.Queue => "set queue to be repeated",
            _ => "disabled loop mode"
        };
    }
}