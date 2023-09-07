using Zeenox.Services;

namespace Zeenox.Models;

public class SocketMessage
{
    private SocketMessage(PlayerData player, TrackData track, QueueData queue)
    {
        Player = player;
        Track = track;
        Queue = queue;
    }

    public PlayerData Player { get; init; }
    public TrackData Track { get; init; }
    public QueueData Queue { get; init; }

    public static SocketMessage FromZeenoxPlayer(
        ZeenoxPlayer player,
        bool updateQueue = false,
        bool updateTrack = false,
        bool updatePlayer = false
    )
    {
        return new SocketMessage(
            updatePlayer ? PlayerData.FromZeenoxPlayer(player) : PlayerData.Empty,
            updateTrack
                ? TrackData.FromZeenoxTrackItem((ZeenoxTrackItem)player.CurrentItem!)
                : TrackData.Empty,
            updateQueue ? QueueData.FromZeenoxPlayer(player) : QueueData.Empty
        );
    }

    public static SocketMessage Empty => new(PlayerData.Empty, TrackData.Empty, QueueData.Empty);
}
