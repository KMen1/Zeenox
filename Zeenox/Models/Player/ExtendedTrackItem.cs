using System.Collections.Immutable;
using Discord;
using Lavalink4NET.Integrations.Lavasrc;
using Lavalink4NET.Integrations.LyricsJava;
using Lavalink4NET.Players;
using Lavalink4NET.Tracks;

namespace Zeenox.Models.Player;

public class ExtendedTrackItem(TrackReference reference, IUser? requestedBy) : ITrackQueueItem
{
    public ExtendedTrackItem(LavalinkTrack track, IUser? requestedBy) : this(new TrackReference(track), requestedBy) { }
    public ExtendedLavalinkTrack Track => new(Reference.Track!);
    public string Title => Track.SourceName == "spotify" ? $"{Track.Author} - {Track.Title}" : Track.Title;
    public string ArtworkUri => Track.ArtworkUri?.ToString() ?? "";
    public IUser? RequestedBy { get; } = requestedBy;
    public ImmutableArray<TimedLyricsLine>? TimedLyrics { get; set; }
    public ImmutableArray<string>? Lyrics { get; set; }
    public TrackReference Reference { get; } = reference;
}