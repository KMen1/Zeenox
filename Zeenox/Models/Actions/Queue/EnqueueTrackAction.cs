using Discord;
using Zeenox.Enums;
using Zeenox.Models.Player;
using Zeenox.Models.Socket;

namespace Zeenox.Models.Actions.Queue;

public class EnqueueTrackAction(IUser user, ExtendedTrackItem trackItem) : QueueAction(user, QueueActionType.AddTrack)
{
    public TrackPayload Track { get; } = new(trackItem);

    public override string Stringify()
    {
        return  $"enqueued: {trackItem.Title}";
    }
}