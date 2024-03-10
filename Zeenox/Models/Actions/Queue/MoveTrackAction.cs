using Discord;
using Zeenox.Dtos;
using Zeenox.Enums;
using Zeenox.Models.Player;

namespace Zeenox.Models.Actions.Queue;

public class MoveTrackAction(IUser user, int from, int to, ExtendedTrackItem trackItem) : QueueAction(user,
    QueueActionType.MoveTrack)
{
    public TrackDTO Track { get; } = new(trackItem);
    public int From { get; } = from;
    public int To { get; } = to;

    public override string Stringify() => $"moved {trackItem.Title} from #{From + 1} to #{To + 1}";
}