using Discord;
using Zeenox.Dtos;
using Zeenox.Models.Player;
using ActionType = Zeenox.Enums.ActionType;

namespace Zeenox.Models.Actions.Queue;

public class MoveTrackAction(IUser user, int from, int to, ExtendedTrackItem trackItem) : Action(user,
    ActionType.MoveTrack)
{
    public TrackDTO Track { get; } = new(trackItem);
    public int From { get; } = from;
    public int To { get; } = to;
}