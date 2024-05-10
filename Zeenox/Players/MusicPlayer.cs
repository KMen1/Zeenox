using System.Collections.Immutable;
using Lavalink4NET;
using Lavalink4NET.Integrations.LyricsJava;
using Lavalink4NET.Integrations.LyricsJava.Players;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Players.Vote;
using Lavalink4NET.Protocol.Payloads.Events;
using Lavalink4NET.Rest.Entities.Tracks;
using Zeenox.Models.Player;

namespace Zeenox.Players;

public abstract class MusicPlayer(IPlayerProperties<MusicPlayer, MusicPlayerOptions> properties)
    : VoteLavalinkPlayer(properties), ILavaLyricsPlayerListener
{
    public new ExtendedTrackItem? CurrentItem => (ExtendedTrackItem?)base.CurrentItem;
    public DateTimeOffset StartedAt { get; } = properties.SystemClock.UtcNow;
    public bool IsAutoPlayEnabled { get; private set; } = true;
    private IAudioService AudioService => properties.Options.Value.AudioService;

    public virtual ValueTask NotifyLyricsLoadedAsync(Lyrics? lyrics, CancellationToken cancellationToken = new())
    {
        if (CurrentItem is null || lyrics is null)
        {
            return ValueTask.CompletedTask;
        }

        CurrentItem.Lyrics = lyrics.Text.Split("\n").ToImmutableArray();
        CurrentItem.TimedLyrics = lyrics.TimedLines;

        return ValueTask.CompletedTask;
    }

    protected virtual async Task<int> PlayAsync(IEnumerable<ExtendedTrackItem> tracksEnumerable)
    {
        var tracks = tracksEnumerable.ToArray();
        if (tracks.Length == 0)
        {
            return 0;
        }

        if (CurrentItem is not null)
        {
            await PlayAsync(tracks[0]).ConfigureAwait(false);
            await Queue.AddRangeAsync(tracks[1..]).ConfigureAwait(false);
            return 1;
        }

        await Queue.AddRangeAsync(tracks[1..]).ConfigureAwait(false);
        await PlayAsync(tracks[0], false).ConfigureAwait(false);
        return 0;
    }

    protected virtual ValueTask SetVolumeAsync(int volume) =>
        SetVolumeAsync((float)Math.Floor(volume / (double)2) / 100f);

    protected virtual async ValueTask<ExtendedTrackItem?> RewindAsync()
    {
        if (!Queue.HasHistory)
        {
            return null;
        }

        var track = Queue.History.LastOrDefault();
        if (track is not ExtendedTrackItem trackItem)
        {
            return null;
        }

        await Queue.History.RemoveAtAsync(Queue.History.Count - 1).ConfigureAwait(false);
        await PlayAsync(trackItem, false).ConfigureAwait(false);
        return trackItem;
    }

    protected virtual Task SetRepeatModeAsync(TrackRepeatMode repeatMode)
    {
        RepeatMode = repeatMode;
        return Task.CompletedTask;
    }

    protected virtual Task ToggleAutoPlayAsync()
    {
        IsAutoPlayEnabled = !IsAutoPlayEnabled;
        return Task.CompletedTask;
    }

    protected virtual Task CycleRepeatModeAsync()
    {
        var shouldDisable = !Enum.IsDefined(typeof(TrackRepeatMode), RepeatMode + 1);
        RepeatMode = shouldDisable ? 0 : RepeatMode + 1;
        return Task.CompletedTask;
    }

    public override ValueTask PauseAsync(CancellationToken cancellationToken = new()) => State == PlayerState.Paused
        ? base.ResumeAsync(cancellationToken)
        : base.PauseAsync(cancellationToken);

    public override ValueTask ResumeAsync(CancellationToken cancellationToken = new()) => State == PlayerState.Paused
        ? base.ResumeAsync(cancellationToken)
        : base.PauseAsync(cancellationToken);

    protected virtual ValueTask<int> ClearQueueAsync() => Queue.ClearAsync();

    protected virtual ValueTask<int> DistinctQueueAsync() => Queue.DistinctAsync();

    protected virtual ValueTask SeekAsync(int position) =>
        SeekAsync(TimeSpan.FromSeconds(position), CancellationToken.None);

    protected virtual async Task ReverseQueueAsync()
    {
        var reversed = Queue.Reverse().ToList();
        await Queue.ClearAsync().ConfigureAwait(false);
        await Queue.AddRangeAsync(reversed).ConfigureAwait(false);
    }

    protected virtual async ValueTask<bool> SkipToAsync(int index)
    {
        if (!IsValidIndex(index))
        {
            return false;
        }

        var track = Queue[index];
        await Queue.RemoveRangeAsync(0, index + 1).ConfigureAwait(false);
        await PlayAsync(track, false).ConfigureAwait(false);
        return true;
    }

    protected virtual ValueTask<bool> RemoveAtAsync(int index) => Queue.RemoveAtAsync(index);

    protected virtual ValueTask ShuffleAsync() => Queue.ShuffleAsync();

    private bool IsValidIndex(int index) => index >= 0 && index < Queue.Count;
    
    public virtual async ValueTask<bool> MoveTrackAsync(int from, int to)
    {
        if (!IsValidIndex(from) || !IsValidIndex(to))
        {
            return false;
        }

        var track = Queue[from];
        await Queue.RemoveAtAsync(from).ConfigureAwait(false);
        await Queue.InsertAsync(to, track).ConfigureAwait(false);
        return true;
    }

    protected override async ValueTask NotifyTrackEndedAsync(ITrackQueueItem queueItem,
                                                             TrackEndReason endReason,
                                                             CancellationToken cancellationToken = new())
    {
        await base.NotifyTrackEndedAsync(queueItem, endReason, cancellationToken).ConfigureAwait(false);

        if (IsAutoPlayEnabled && Queue.Count < 3 && RepeatMode == TrackRepeatMode.None)
        {
            var recommendedTracks = await GetRecommendedTrackAsync(CurrentItem is null ? 4 : 3 - Queue.Count)
                .ConfigureAwait(false);
            await PlayAsync(recommendedTracks).ConfigureAwait(false);
        }
    }

    private async Task<List<ExtendedTrackItem>> GetRecommendedTrackAsync(int limit = 5)
    {
        var trackIds = GetSeedTrackIds();

        var rec = await AudioService.Tracks.LoadTracksAsync(
            $"sprec:seed_tracks={string.Join(',', trackIds)}&min_popularity=50", new TrackLoadOptions
            {
                SearchBehavior = StrictSearchBehavior.Passthrough,
                SearchMode = TrackSearchMode.None
            }).ConfigureAwait(false);
        
        return rec.Tracks.Select(x => new ExtendedTrackItem(x, null)).Take(limit).ToList();
    }

    private ImmutableArray<string> GetSeedTrackIds()
    {
        var ids = new List<string>();
        for (var i = 0; i < 4; i++)
        {
            if (i < Queue.Count && Queue[i].Track?.SourceName == "spotify")
            {
                ids.Add(Queue[i].Identifier);
            }
            else if (i < Queue.History?.Count && Queue.History[i].Track?.SourceName == "spotify")
            {
                ids.Add(Queue.History[i].Identifier);
            }
            else
            {
                break;
            }
        }
        return [..ids];
    }
}