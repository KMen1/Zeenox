using System.Collections.Immutable;
using Lavalink4NET;
using Lavalink4NET.Integrations.LyricsJava;
using Lavalink4NET.Integrations.LyricsJava.Players;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Players.Vote;
using Lavalink4NET.Protocol.Payloads.Events;
using Lavalink4NET.Rest.Entities.Tracks;
using SpotifyAPI.Web;
using Zeenox.Models.Player;

namespace Zeenox.Players;

public abstract class MusicPlayer(IPlayerProperties<MusicPlayer, MusicPlayerOptions> properties)
    : VoteLavalinkPlayer(properties), ILavaLyricsPlayerListener
{
    public new ExtendedTrackItem? CurrentItem => (ExtendedTrackItem?)base.CurrentItem;
    public DateTimeOffset StartedAt { get; } = properties.SystemClock.UtcNow;
    public bool IsAutoPlayEnabled { get; private set; } = true;
    private SpotifyClient SpotifyClient => properties.Options.Value.SpotifyClient;
    private IAudioService AudioService => properties.Options.Value.AudioService;
    private string SpotifyMarket => properties.Options.Value.SpotifyMarket;

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
        if (index < 0 || index >= Queue.Count)
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

    public virtual async ValueTask<bool> MoveTrackAsync(int from, int to)
    {
        if (from < 0 || from >= Queue.Count)
        {
            return false;
        }

        if (to < 0 || to >= Queue.Count)
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

        if (IsAutoPlayEnabled && Queue.Count < 2 && RepeatMode == TrackRepeatMode.None)
        {
            var recommendedTracks = await GetRecommendedTrackAsync(CurrentItem is null ? 3 : 2 - Queue.Count)
                .ConfigureAwait(false);
            await PlayAsync(recommendedTracks).ConfigureAwait(false);
        }
    }

    private async Task<List<ExtendedTrackItem>> GetRecommendedTrackAsync(int limit = 2)
    {
        var trackIds = GetSeedTrackIds();
        var request = new RecommendationsRequest
        {
            Market = SpotifyMarket
        };
        request.SeedTracks.AddRange(trackIds);
        request.Min.Add("popularity", "50");

        var response = await SpotifyClient.Browse.GetRecommendations(request).ConfigureAwait(false);
        var orderedTracks = response.Tracks.OrderByDescending(x => x.Popularity).ToList();

        var recommendedTracks = new List<ExtendedTrackItem>();
        for (var i = 0; i < limit; i++)
        {
            var url = orderedTracks.Skip(i).First(x => Queue.All(y => y.Identifier != x.Id)).ExternalUrls["spotify"];
            var track = await AudioService.Tracks
                                          .LoadTrackAsync(
                                              url, new TrackLoadOptions { SearchMode = TrackSearchMode.None })
                                          .ConfigureAwait(false);
            if (track is null)
            {
                continue;
            }

            recommendedTracks.Add(new ExtendedTrackItem(track, null));
        }

        return recommendedTracks;
    }

    private List<string> GetSeedTrackIds()
    {
        var ids = new List<string>();
        var count = 0;
        var index1 = 0;
        var index2 = 0;

        while (count < 4 && (index1 < Queue.Count || index2 < Queue.History?.Count))
        {
            if (index1 < Queue.Count)
            {
                if (Queue[index1].Track?.SourceName == "spotify")
                {
                    ids.Add(Queue[index1].Identifier);
                    count++;
                }

                index1++;
            }

            if (!(index2 < Queue.History?.Count))
            {
                continue;
            }

            if (Queue.History[index2].Track?.SourceName != "spotify")
            {
                index2++;
                continue;
            }

            ids.Add(Queue.History[index2].Identifier);
            index2++;
            count++;
        }

        return ids;
    }
}