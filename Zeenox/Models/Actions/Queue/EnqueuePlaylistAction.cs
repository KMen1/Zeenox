using Discord;
using Lavalink4NET.Rest.Entities.Tracks;
using Zeenox.Enums;
using Zeenox.Models.Player;
using Zeenox.Models.Socket;

namespace Zeenox.Models.Actions.Queue;

public class EnqueuePlaylistAction(IUser user, PlaylistInformation? playlistInformation, IEnumerable<ExtendedTrackItem> trackItems) : QueueAction(user, QueueActionType.AddPlaylist)
{
    public string? Name { get; } = playlistInformation?.Name;
    public string? Url { get; } = (playlistInformation?.AdditionalInformation.ContainsKey("url")).GetValueOrDefault() ? 
        playlistInformation?.AdditionalInformation["url"].ToString() : null;
    public string? ArtworkUrl { get; } = (playlistInformation?.AdditionalInformation.ContainsKey("artworkUrl")).GetValueOrDefault() ? 
        playlistInformation?.AdditionalInformation["artworkUrl"].ToString() : null;
    public string? Author { get; } = (playlistInformation?.AdditionalInformation.ContainsKey("author")).GetValueOrDefault() ? 
        playlistInformation?.AdditionalInformation["author"].ToString() : null;
    public List<TrackPayload> Tracks { get; } = trackItems.Select(x => new TrackPayload(x)).ToList();
    public int Count => Tracks.Count;
    
    public override string Stringify()
    {
        return $"enqueued {Count} tracks";
    }
}