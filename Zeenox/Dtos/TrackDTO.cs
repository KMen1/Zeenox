using System.Text.Json.Serialization;
using Lavalink4NET.Tracks;
using Zeenox.Models.Player;

namespace Zeenox.Dtos;

public class TrackDTO
{
    public string Id { get; } = null!;
    public string Title { get; } = null!;
    public string Author { get; } = null!;
    public int Duration { get; }
    public string? Url { get; }
    public string? ArtworkUrl { get; }
    public SocketUserDTO? RequestedBy { get; }

    public TrackDTO(ExtendedTrackItem trackItem)
    {
        Id = trackItem.Track.Track.Identifier;
        Title = trackItem.Track.Title;
        Author = trackItem.Track.Author;
        Duration = (int)trackItem.Track.Duration.TotalSeconds;
        RequestedBy = trackItem.RequestedBy is not null
            ? new SocketUserDTO(trackItem.RequestedBy)
            : null;
        Url = trackItem.Track.Uri?.ToString();
        ArtworkUrl = trackItem.ArtworkUri;
    }

    public TrackDTO(LavalinkTrack track)
    {
        Id = track.Identifier;
        Title = track.Title;
        Author = track.Author;
        Duration = (int)track.Duration.TotalSeconds;
        Url = track.Uri?.ToString();
        ArtworkUrl = track.ArtworkUri?.ToString();
    }

    [JsonConstructor]
    public TrackDTO() { }
}
