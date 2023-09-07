namespace Zeenox.Models;

public class TrackData
{
    private TrackData(
        bool shouldUpdate,
        string title,
        string? url,
        string? thumbnail,
        int duration,
        DiscordUserData requestedBy
    )
    {
        ShouldUpdate = shouldUpdate;
        Title = title;
        Url = url;
        Thumbnail = thumbnail;
        Duration = duration;
        RequestedBy = requestedBy;
    }

    public bool ShouldUpdate { get; init; }
    public string Title { get; init; }
    public string? Url { get; init; }
    public string? Thumbnail { get; init; }
    public int Duration { get; init; }
    public DiscordUserData RequestedBy { get; init; }

    public static TrackData FromZeenoxTrackItem(ZeenoxTrackItem trackItem)
    {
        var track = trackItem.Reference.Track!;
        return new TrackData(
            true,
            track.GetTitle(),
            track.Uri?.ToString(),
            trackItem.GetThumbnailUrl(),
            (int)track.Duration.TotalSeconds,
            DiscordUserData.FromUser(trackItem.RequestedBy)
        );
    }

    public static TrackData Empty =>
        new(false, string.Empty, string.Empty, string.Empty, 0, DiscordUserData.Empty);
}
