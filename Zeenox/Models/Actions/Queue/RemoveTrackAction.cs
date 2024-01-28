using Discord;
using Zeenox.Enums;
using Zeenox.Models.Player;
using Zeenox.Models.Socket;

namespace Zeenox.Models.Actions.Queue;

public class RemoveTrackAction(IUser user, ExtendedTrackItem trackItem) : QueueAction(user, QueueActionType.RemoveTrack)
{
    public TrackPayload Track { get; } = new(trackItem);
    
    public override string Stringify()
    {
        return $"Removed {trackItem.Title} from the queue.";
    }
}