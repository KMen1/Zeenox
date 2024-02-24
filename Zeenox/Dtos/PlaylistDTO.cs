using Lavalink4NET.Rest.Entities.Tracks;

namespace Zeenox.Dtos;

public class PlaylistDTO(string name, string url, string artworkUrl, string author)
{
    public string Name { get; } = name;
    public string Url { get; } = url;
    public string ArtworkUrl { get; } = artworkUrl;
    public string Author { get; } = author;

    public PlaylistDTO(TrackLoadResult result) : this(result.Playlist!.Name, result.Playlist.AdditionalInformation["url"].ToString(), result.Playlist.AdditionalInformation["artworkUrl"].ToString(), result.Playlist.AdditionalInformation["author"].ToString()) {}
    
    public PlaylistDTO(PlaylistInformation playlistInformation) : this(playlistInformation.Name, playlistInformation.AdditionalInformation["url"].ToString(), playlistInformation.AdditionalInformation["artworkUrl"].ToString(), playlistInformation.AdditionalInformation["author"].ToString()) {}
}