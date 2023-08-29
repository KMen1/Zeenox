using Discord;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;

namespace Zeenox.Models;

public class ZeenoxTrackItem : ITrackQueueItem
{
    public ZeenoxTrackItem(TrackReference reference, IUser requestedBy)
    {
        Reference = reference;
        RequestedBy = requestedBy;
    }

    public TrackReference Reference { get; }
    public IUser RequestedBy { get; }
}
