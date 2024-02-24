using Discord;
using Zeenox.Dtos;
using Zeenox.Enums;
using Zeenox.Models.Player;

namespace Zeenox.Models.Actions.Queue;

public class RemoveTrackAction(IUser user, ExtendedTrackItem trackItem) : QueueAction(user, QueueActionType.RemoveTrack)
{
    public TrackDTO Track { get; } = new(trackItem);
    
    public override string Stringify()
    {
        return $"Removed {trackItem.Title} from the queue.";
    }
}