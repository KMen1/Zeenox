using Discord;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;
using Zeenox.Enums;
using Zeenox.Models.Actions.Player;
using Zeenox.Models.Actions.Queue;
using Zeenox.Models.Player;
using Action = Zeenox.Models.Actions.Action;
using ActionType = Zeenox.Enums.ActionType;

namespace Zeenox.Players;

public class LoggedPlayer
    (IPlayerProperties<LoggedPlayer, EmbedPlayerOptions> properties) : EmbedPlayer(properties)
{
    private List<Action> Actions { get; } = [];
    public ExtendedTrackItem? LastCurrentItem { get; private set; }

    protected virtual Task AddActionAsync(Action action)
    {
        Actions.Add(action);
        return Task.CompletedTask;
    }
    
    public object GetActionsForSerialization()
    {
        return Actions.Select(x => (object)x);
    }
    
    public object GetActionForSerialization()
    {
        return Actions.Last();
    }

    public IEnumerable<string> StringifyActions()
    {
        return Actions.Select(action => action.StringifyFull());
    }

    public virtual async Task<int> PlayAsync(IUser user, IEnumerable<ExtendedTrackItem> tracksEnumerable)
    {
        var tracks = tracksEnumerable.ToList();
        await AddActionAsync(new EnqueuePlaylistAction(user, null, tracks)).ConfigureAwait(false);
        var result = await base.PlayAsync(tracks).ConfigureAwait(false);
        return result;
    }
    
    public virtual async Task<int> PlayAsync(IUser user, TrackLoadResult trackLoadResult)
    {
        var tracks = trackLoadResult.Tracks.Select(x => new ExtendedTrackItem(x, user)).ToList();
        await AddActionAsync(new EnqueuePlaylistAction(user, trackLoadResult.Playlist, tracks)).ConfigureAwait(false);
        var result = await base.PlayAsync(tracks).ConfigureAwait(false);
        return result;
    }

    public virtual async ValueTask<int> PlayAsync(IUser user, ExtendedTrackItem trackItem, bool enqueue = true)
    {
        var result = await base.PlayAsync(trackItem, enqueue).ConfigureAwait(false);
        if (result > 0)
            await AddActionAsync(new EnqueueTrackAction(user, trackItem)).ConfigureAwait(false);
        else
            await AddActionAsync(new PlayAction(user, trackItem)).ConfigureAwait(false);
        return result;
    }
    
    public virtual async ValueTask ResumeAsync(IUser user)
    {
        await base.ResumeAsync(CancellationToken.None).ConfigureAwait(false);
        await AddActionAsync(new ResumeAction(user)).ConfigureAwait(false);
    }

    public virtual async ValueTask PauseAsync(IUser user)
    {
        await base.PauseAsync(CancellationToken.None).ConfigureAwait(false);
        await AddActionAsync(new PauseAction(user)).ConfigureAwait(false);
    }

    public virtual async Task SetRepeatModeAsync(IUser user, TrackRepeatMode repeatMode)
    {
        await base.SetRepeatModeAsync(repeatMode).ConfigureAwait(false);
        await AddActionAsync(new RepeatAction(user, repeatMode)).ConfigureAwait(false);
    }

    public virtual async Task CycleRepeatModeAsync(IUser user)
    {
        await base.CycleRepeatModeAsync().ConfigureAwait(false);
        await AddActionAsync(new RepeatAction(user, RepeatMode)).ConfigureAwait(false);
    }

    public virtual async Task ToggleAutoPlayAsync(IUser user)
    {
        await base.ToggleAutoPlayAsync().ConfigureAwait(false);
        await AddActionAsync(new ToggleAutoPlayAction(user, IsAutoPlayEnabled)).ConfigureAwait(false);
    }

    public virtual async ValueTask<int> ClearQueueAsync(IUser user)
    {
        var result = await base.ClearQueueAsync().ConfigureAwait(false);
        await AddActionAsync(new QueueAction(user, QueueActionType.Clear)).ConfigureAwait(false);
        return result;
    }

    public virtual async ValueTask<int> DistinctQueueAsync(IUser user)
    {
        var result = await base.DistinctQueueAsync().ConfigureAwait(false);
        await AddActionAsync(new QueueAction(user, QueueActionType.Distinct)).ConfigureAwait(false);
        return result;
    }

    public virtual async Task ReverseQueueAsync(IUser user)
    {
        await base.ReverseQueueAsync().ConfigureAwait(false);
        await AddActionAsync(new QueueAction(user, QueueActionType.Reverse)).ConfigureAwait(false);
    }

    public async ValueTask SkipAsync(IUser user)
    {
        var nextTrack = Queue.FirstOrDefault();
        if (nextTrack is not null && CurrentItem is not null)
            await AddActionAsync(new SkipAction(user, CurrentItem, (ExtendedTrackItem)nextTrack)).ConfigureAwait(false);
        await base.SkipAsync().ConfigureAwait(false);
    }

    public virtual async Task RewindAsync(IUser user)
    {
        var result = await base.RewindAsync().ConfigureAwait(false);
        if (result is not null)
            await AddActionAsync(new RewindAction(user, result)).ConfigureAwait(false);
    }

    public virtual async ValueTask SeekAsync(IUser user, int position)
    {
        await base.SeekAsync(position).ConfigureAwait(false);
        await AddActionAsync(new SeekAction(user, position)).ConfigureAwait(false);
    }

    public async ValueTask<bool> SkipToAsync(IUser user, int index)
    {
        var prevTrack = CurrentItem;
        var result = await base.SkipToAsync(index).ConfigureAwait(false);
        if (result)
            await AddActionAsync(new SkipToAction(user, prevTrack!, CurrentItem!)).ConfigureAwait(false);
        return result;
    }

    public virtual async ValueTask<bool> RemoveAsync(IUser user, int index)
    {
        var track = Queue.ElementAtOrDefault(index) as ExtendedTrackItem;
        var result = await base.RemoveAtAsync(index).ConfigureAwait(false);
        if (result)
            await AddActionAsync(new RemoveTrackAction(user, track!)).ConfigureAwait(false);
        return result;
    }

    public virtual async ValueTask ShuffleAsync(IUser user)
    {
        await base.ShuffleAsync().ConfigureAwait(false);
        await AddActionAsync(new QueueAction(user, QueueActionType.Shuffle)).ConfigureAwait(false);
    }

    public virtual async ValueTask<bool> MoveTrackAsync(IUser user, int from, int to)
    {
        var track = Queue.ElementAtOrDefault(from) as ExtendedTrackItem;
        var result = await base.MoveTrackAsync(from, to).ConfigureAwait(false);
        if (result)
            await AddActionAsync(new MoveTrackAction(user, from, to, track!)).ConfigureAwait(false);
        return result;
    }

    public virtual async ValueTask<bool> RemoveAtAsync(IUser user, int index)
    {
        var track = Queue.ElementAtOrDefault(index) as ExtendedTrackItem;
        var result = await base.RemoveAtAsync(index).ConfigureAwait(false);
        if (result)
            await AddActionAsync(new RemoveTrackAction(user, track!)).ConfigureAwait(false);
        return result;
    }

    public virtual async ValueTask SetVolumeAsync(
        IUser user,
        int volume
    )
    {
        var prevVolume = Volume * 200;
        await base.SetVolumeAsync(volume).ConfigureAwait(false);
        await AddActionAsync(new VolumeAction(user, volume, prevVolume > volume ? ActionType.VolumeDown : ActionType.VolumeUp)).ConfigureAwait(false);
    }

    public virtual async ValueTask StopAsync(IUser user)
    {
        LastCurrentItem = CurrentItem;
        await base.StopAsync().ConfigureAwait(false);
        await AddActionAsync(new StopAction(user)).ConfigureAwait(false);
    }
}