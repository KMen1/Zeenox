using Discord;
using Zeenox.Enums;
using Zeenox.Models.Player;
using Zeenox.Models.Socket;

namespace Zeenox.Models.Actions.Queue;

public class MoveTrackAction(IUser user, int from, int to, ExtendedTrackItem trackItem) : QueueAction(user,
    QueueActionType.MoveTrack)
{
    public TrackDto Track { get; } = new(trackItem);
    public int From { get; } = from;
    public int To { get; } = to;
    
    public override string Stringify()
    {
        return $"moved {trackItem.Title} from #{from + 1} to #{to + 1}";
    }
}