using Lavalink4NET.Tracks;
using Zeenox.Dtos;
using Zeenox.Enums;
using Zeenox.Models.Player;

namespace Zeenox.Models.Socket;

public class UpdateTrackPayload(TrackDTO? trackDto) : IPayload
{
    public PayloadType Type { get; } = PayloadType.UpdateTrack;
    public TrackDTO? Track { get; } = trackDto;
    
    public UpdateTrackPayload(ExtendedTrackItem? track) : this(track is not null ? new TrackDTO(track) : null)
    {
    }
    
    public UpdateTrackPayload(LavalinkTrack track) : this(new TrackDTO(track))
    {
    }
}
