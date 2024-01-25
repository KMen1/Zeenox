using Discord;
using Lavalink4NET.Integrations.Lavasrc;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Tracks;

namespace Zeenox.Models.Player;

public class ExtendedTrackItem(TrackReference reference, IUser? requestedBy) : ITrackQueueItem
{
    public TrackReference Reference { get; } = reference;
    public ExtendedLavalinkTrack Track => new(Reference.Track!);
    public string Title => Track.SourceName == "spotify" ? $"{Track.Author} - {Track.Title}" : Track.Title;
    public string AlbumImageUrl => Track.ArtworkUri?.ToString() ?? "";
    public IUser? RequestedBy { get; } = requestedBy;
    public string? Lyrics { get; set; }
    
    public ExtendedTrackItem(LavalinkTrack track, IUser? requestedBy) : this(new TrackReference(track), requestedBy) { }
}
