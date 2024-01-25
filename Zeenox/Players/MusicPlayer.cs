﻿using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Players.Vote;
using Zeenox.Models.Player;

namespace Zeenox.Players;

public abstract class MusicPlayer(IPlayerProperties<MusicPlayer, VoteLavalinkPlayerOptions> properties)
    : VoteLavalinkPlayer(properties)
{
    public new ExtendedTrackItem? CurrentItem => (ExtendedTrackItem?)base.CurrentItem;
    public DateTimeOffset StartedAt { get; } = properties.SystemClock.UtcNow;

    protected virtual async Task<int> PlayAsync(IEnumerable<ExtendedTrackItem> tracksEnumerable)
    {
        var tracks = tracksEnumerable.ToArray();
        if (tracks.Length == 0)
            return 0;
        
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

    protected virtual ValueTask SetVolumeAsync(int volume)
    {
        return SetVolumeAsync((float)Math.Floor(volume / (double)2) / 100f);
    }

    protected virtual async ValueTask<ExtendedTrackItem?> RewindAsync()
    {
        if (!Queue.HasHistory)
            return null;

        var track = Queue.History.LastOrDefault();
        if (track is not ExtendedTrackItem trackItem)
            return null;
        await Queue.History.RemoveAtAsync(Queue.History.Count - 1).ConfigureAwait(false);
        await PlayAsync(trackItem, false).ConfigureAwait(false);
        return trackItem;
    }

    protected virtual Task SetRepeatModeAsync(TrackRepeatMode repeatMode)
    {
        RepeatMode = repeatMode;
        return Task.CompletedTask;
    }

    protected virtual Task CycleRepeatModeAsync()
    {
        var shouldDisable = !Enum.IsDefined(typeof(TrackRepeatMode), RepeatMode + 1);
        RepeatMode = shouldDisable ? 0 : RepeatMode + 1;
        return Task.CompletedTask;
    }

    public override ValueTask PauseAsync(CancellationToken cancellationToken = new())
    {
        return State == PlayerState.Paused ? base.ResumeAsync(cancellationToken) : base.PauseAsync(cancellationToken);
    }

    public override ValueTask ResumeAsync(CancellationToken cancellationToken = new())
    {
        return State == PlayerState.Paused ? base.ResumeAsync(cancellationToken) : base.PauseAsync(cancellationToken);
    }

    protected virtual ValueTask<int> ClearQueueAsync()
    {
        return Queue.ClearAsync();
    }

    protected virtual ValueTask<int> DistinctQueueAsync()
    {
        return Queue.DistinctAsync();
    }

    protected virtual ValueTask SeekAsync(int position)
    {
        return SeekAsync(TimeSpan.FromSeconds(position), CancellationToken.None);
    }

    protected virtual async Task ReverseQueueAsync()
    {
        var reversed = Queue.Reverse().ToList();
        await Queue.ClearAsync().ConfigureAwait(false);
        await Queue.AddRangeAsync(reversed).ConfigureAwait(false);
    }

    protected virtual async ValueTask<bool> SkipToAsync(int index)
    {
        if (index < 0 || index >= Queue.Count)
            return false;

        var track = Queue[index];
        await Queue.RemoveRangeAsync(0, index + 1).ConfigureAwait(false);
        await PlayAsync(track, false).ConfigureAwait(false);
        return true;
    }

    protected virtual ValueTask<bool> RemoveAtAsync(int index)
    {
        return Queue.RemoveAtAsync(index);
    }

    protected virtual ValueTask ShuffleAsync()
    {
        return Queue.ShuffleAsync();
    }
    
    public void SetLyrics(string? lyrics)
    {
        if (CurrentItem is not null)
            CurrentItem.Lyrics = lyrics;
    }

    public virtual async ValueTask<bool> MoveTrackAsync(int from, int to)
    {
        if (from < 0 || from >= Queue.Count)
            return false;

        if (to < 0 || to >= Queue.Count)
            return false;

        var track = Queue[from];
        await Queue.RemoveAtAsync(from).ConfigureAwait(false);
        await Queue.InsertAsync(to, track).ConfigureAwait(false);
        return true;
    }
}