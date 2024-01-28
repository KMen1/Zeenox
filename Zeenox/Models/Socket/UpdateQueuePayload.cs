using Lavalink4NET.Players.Queued;
using Zeenox.Enums;
using Zeenox.Models.Player;

namespace Zeenox.Models.Socket;

public class UpdateQueuePayload(List<TrackPayload> tracks, List<TrackPayload> history) : IPayload
{
    public PayloadType Type { get; } = PayloadType.UpdateQueue;
    public List<TrackPayload> Tracks { get; } = tracks;
    public List<TrackPayload> History { get; } = history;

    public UpdateQueuePayload(IQueuedLavalinkPlayer player) : this(player.Queue.Select(x => new TrackPayload((ExtendedTrackItem)x))
        .ToList(), [])
    {
    }
}