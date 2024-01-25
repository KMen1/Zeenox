using Zeenox.Models.Player;

namespace Zeenox.Models;

public class ExtendedTrackDto(string id, ulong requesterId)
{
    public string Id { get; set; } = id;
    public ulong RequesterId { get; set; } = requesterId;
    
    public ExtendedTrackDto(ExtendedTrackItem track) : this(track.Track.Track.ToString(), track.RequestedBy?.Id ?? 0) { }
}