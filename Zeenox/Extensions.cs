using Lavalink4NET.Integrations.Lavasrc;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Tracks;
using Zeenox.Models;

namespace Zeenox;

public static class Extensions
{
    public static string GetTitle(this LavalinkTrack track)
    {
        return track.SourceName == "spotify" ? $"{track.Author} - {track.Title}" : track.Title;
    }

    public static string GetTitle(this ITrackQueueItem queueItem)
    {
        var track = queueItem.Track!;
        return track.SourceName == "spotify" ? $"{track.Author} - {track.Title}" : track.Title;
    }

    public static string? GetThumbnailUrl(this ITrackQueueItem queueItem)
    {
        var track = queueItem.Track!;
        return new ExtendedLavalinkTrack(track).ArtworkUri?.ToString();
    }

    public static string ToTimeString(this TimeSpan timeSpan)
    {
        return timeSpan.TotalHours < 1
            ? timeSpan.ToString(@"mm\:ss")
            : timeSpan.ToString(timeSpan.TotalDays < 1 ? @"hh\:mm\:ss" : @"dd\:hh\:mm\:ss");
    }
}
