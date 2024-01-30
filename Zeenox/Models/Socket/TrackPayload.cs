using System.Text.Json.Serialization;
using Zeenox.Enums;
using Zeenox.Models.Player;

namespace Zeenox.Models.Socket;

public class TrackPayload : IPayload
{
    public PayloadType Type { get; } = PayloadType.UpdateTrack;
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public int? Duration { get; set; }
    public BasicDiscordUser? RequestedBy { get; set; }
    public string? Url { get; set; }
    public string? Thumbnail { get; set; }

    public TrackPayload(ExtendedTrackItem? trackItem)
    {
        Id = trackItem?.Track.Track.Identifier;
        Title = trackItem?.Track.Title;
        Author = trackItem?.Track.Author;
        Duration = (int?)trackItem?.Track.Duration.TotalSeconds;
        RequestedBy = new BasicDiscordUser(trackItem?.RequestedBy);
        Url = trackItem?.Track.Uri?.ToString();
        Thumbnail = trackItem?.AlbumImageUrl;
    }

    [JsonConstructor]
    public TrackPayload() { }
}
