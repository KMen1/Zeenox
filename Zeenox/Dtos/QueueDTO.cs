using Lavalink4NET.Players.Queued;
using Zeenox.Models.Player;

namespace Zeenox.Dtos;

public class QueueDTO(List<TrackDTO> tracks, List<TrackDTO> history)
{
    public QueueDTO(ITrackQueue queue) : this(queue.Select(x => new TrackDTO((ExtendedTrackItem)x))
                                                   .ToList(),
                                              queue.History?.Select(x => new TrackDTO((ExtendedTrackItem)x)).ToList() ??
                                              []) { }

    public List<TrackDTO> Tracks { get; } = tracks;
    public List<TrackDTO> History { get; } = history;
}