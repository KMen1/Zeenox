using System.Text.Json.Serialization;
using Zeenox.Models.Player;

namespace Zeenox.Models.Socket;

public class TrackDto : ISocketMessageData
{
    public string Type { get; } = "player-track";
    public string Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public int Duration { get; set; }
    public UserDto? RequestedBy { get; set; }
    public string? Url { get; set; }
    public string? Thumbnail { get; set; }

    public TrackDto(ExtendedTrackItem? trackItem)
    {
        Id = trackItem?.Track.Track.Identifier ?? "";
        Title = trackItem?.Track.Title ?? "";
        Author = trackItem?.Track.Author ?? "";
        Duration = (int)(trackItem?.Track.Duration.TotalSeconds ?? 0);
        RequestedBy = new UserDto(trackItem?.RequestedBy);
        Url = trackItem?.Track.Uri?.ToString();
        Thumbnail = trackItem?.AlbumImageUrl;
    }

    [JsonConstructor]
    public TrackDto() { }
}
