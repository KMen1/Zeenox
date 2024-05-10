using Discord;
using Lavalink4NET.Players.Queued;
using ActionType = Zeenox.Enums.ActionType;

namespace Zeenox.Models.Actions.Player;

public class RepeatAction(IUser user, TrackRepeatMode loopMode) : Action(user, ActionType.ChangeLoopMode)
{
    public TrackRepeatMode TrackRepeatMode { get; set; } = loopMode;
}