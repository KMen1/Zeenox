using Lavalink4NET.Players.Queued;
using Zeenox.Models.Player;

namespace Zeenox.Models.Socket;

public class QueueDto(List<TrackDto> tracks) : ISocketMessageData
{
    public string Type { get; } = "player-queue";
    public List<TrackDto> Tracks { get; } = tracks;

    public QueueDto(IQueuedLavalinkPlayer player) : this(player.Queue.Select(x => new TrackDto((ExtendedTrackItem)x))
        .ToList())
    {
    }
}