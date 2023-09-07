using Zeenox.Services;

namespace Zeenox.Models;

public class QueueData
{
    private QueueData(List<TrackData> tracks)
    {
        Tracks = tracks;
    }

    public List<TrackData> Tracks { get; init; }

    public static QueueData FromZeenoxPlayer(ZeenoxPlayer player)
    {
        return new QueueData(
            player.Queue.Select(x => TrackData.FromZeenoxTrackItem((ZeenoxTrackItem)x)).ToList()
        );
    }

    public static QueueData Empty => new(new List<TrackData>());
}
