using Zeenox.Services;

namespace Zeenox.Models;

public class QueueData
{
    private QueueData(bool shouldUpdate, List<TrackData> tracks)
    {
        ShouldUpdate = shouldUpdate;
        Tracks = tracks;
    }

    public bool ShouldUpdate { get; init; }
    public List<TrackData> Tracks { get; init; }

    public static QueueData FromZeenoxPlayer(ZeenoxPlayer player)
    {
        return new QueueData(
            true,
            player.Queue.Select(x => TrackData.FromZeenoxTrackItem((ZeenoxTrackItem)x)).ToList()
        );
    }

    public static QueueData Empty => new(false, new List<TrackData>());
}
