using System.Collections.Concurrent;
using System.Net.WebSockets;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Protocol.Payloads.Events;
using Zeenox.Models.Player;

namespace Zeenox.Players;

public class SocketPlayer
    (IPlayerProperties<SocketPlayer, InteractivePlayerOptions> properties) : InteractivePlayer(properties)
{
    private readonly ConcurrentDictionary<ulong, WebSocket> _webSockets = new();

    protected override async Task<int> PlayAsync(IEnumerable<ExtendedTrackItem> tracksEnumerable)
    {
        var result = await base.PlayAsync(tracksEnumerable).ConfigureAwait(false);
        await UpdateSocketsAsync(updateQueue: true).ConfigureAwait(false);
        return result;
    }

    public override async ValueTask PauseAsync(CancellationToken cancellationToken = new())
    {
        await base.PauseAsync(cancellationToken).ConfigureAwait(false);
        await UpdateSocketsAsync(updatePlayer: true).ConfigureAwait(false);
    }

    public override async ValueTask ResumeAsync(CancellationToken cancellationToken = new())
    {
        await base.ResumeAsync(cancellationToken).ConfigureAwait(false);
        await UpdateSocketsAsync(updatePlayer: true).ConfigureAwait(false);
    }

    public override async ValueTask<bool> MoveTrackAsync(int from, int to)
    {
        var result = await base.MoveTrackAsync(from, to).ConfigureAwait(false);
        if (result)
            await UpdateSocketsAsync(updateQueue: true).ConfigureAwait(false);
        return result;
    }

    protected override async ValueTask SetVolumeAsync(int volume)
    {
        await base.SetVolumeAsync(volume).ConfigureAwait(false);
        await UpdateSocketsAsync(updatePlayer: true).ConfigureAwait(false);
    }

    protected override async Task SetRepeatModeAsync(TrackRepeatMode repeatMode)
    {
        await base.SetRepeatModeAsync(repeatMode).ConfigureAwait(false);
        await UpdateSocketsAsync(updatePlayer: true).ConfigureAwait(false);
    }

    protected override async Task CycleRepeatModeAsync()
    {
        await base.CycleRepeatModeAsync().ConfigureAwait(false);
        await UpdateSocketsAsync(updatePlayer: true).ConfigureAwait(false);
    }

    protected override async ValueTask ShuffleAsync()
    {
        await base.ShuffleAsync().ConfigureAwait(false);
        await UpdateSocketsAsync(updateQueue: true).ConfigureAwait(false);
    }

    protected override async ValueTask<int> DistinctQueueAsync()
    {
        var result = await base.DistinctQueueAsync().ConfigureAwait(false);
        await UpdateSocketsAsync(updateQueue: true).ConfigureAwait(false);
        return result;
    }

    protected override async ValueTask<int> ClearQueueAsync()
    {
        var result = await base.ClearQueueAsync().ConfigureAwait(false);
        await UpdateSocketsAsync(updateQueue: true).ConfigureAwait(false);
        return result;
    }

    protected override async Task ReverseQueueAsync()
    {
        await base.ReverseQueueAsync().ConfigureAwait(false);
        await UpdateSocketsAsync(updateQueue: true).ConfigureAwait(false);
    }

    protected override async ValueTask<bool> SkipToAsync(int index)
    {
        var result = await base.SkipToAsync(index).ConfigureAwait(false);
        if (result)
            await UpdateSocketsAsync(updateQueue: true).ConfigureAwait(false);
        return result;
    }

    protected override async ValueTask<bool> RemoveAtAsync(int index)
    {
        var result = await base.RemoveAtAsync(index).ConfigureAwait(false);
        if (result)
            await UpdateSocketsAsync(updateQueue: true).ConfigureAwait(false);
        return result;
    }

    public void AddSocket(ulong userId, WebSocket socket)
    {
        RemoveSocket(userId);
        _webSockets.TryAdd(userId, socket);
    }

    public void RemoveSocket(ulong userId)
    {
        if (!_webSockets.ContainsKey(userId)) return;
        _webSockets.TryRemove(userId, out _);
    }

    private void RemoveDeadSockets()
    {
        var closedSockets = _webSockets.Where(kvp => kvp.Value.State != WebSocketState.Open).ToList();
        foreach (var item in closedSockets)
        {
            RemoveSocket(item.Key);
        }
    }

    public async Task UpdateSocketsAsync(
        bool updatePlayer = false,
        bool updateTrack = false,
        bool updateQueue = false,
        bool updateActions = false
    )
    {
        if (_webSockets.IsEmpty)
            return;
        //RemoveDeadSockets();

        foreach (var (_, socket) in _webSockets)
        {
            await socket.SendSocketMessagesAsync(this, updatePlayer, updateTrack, updateQueue, updateActions).ConfigureAwait(false);
        }
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        foreach (var (_, socket) in _webSockets)
        {
            if (socket.State == WebSocketState.Open)
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disposed", CancellationToken.None).ConfigureAwait(false);
            socket.Dispose();
        }

        await base.DisposeAsyncCore().ConfigureAwait(false);
    }

    protected override async ValueTask NotifyTrackStartedAsync(ITrackQueueItem queueItem,
        CancellationToken cancellationToken = new())
    {
        await base.NotifyTrackStartedAsync(queueItem, cancellationToken).ConfigureAwait(false);
        await UpdateSocketsAsync(updateTrack: true, updateQueue: true, updatePlayer: true).ConfigureAwait(false);
    }

    protected override async ValueTask NotifyTrackEndedAsync(ITrackQueueItem queueItem, TrackEndReason endReason,
        CancellationToken cancellationToken = new())
    {
        await base.NotifyTrackEndedAsync(queueItem, endReason, cancellationToken).ConfigureAwait(false);
        var task = Queue.Count == 0 ? UpdateSocketsAsync(true, true, true) : Task.CompletedTask;
        await task.ConfigureAwait(false);
    }

    protected override async ValueTask NotifyTrackEnqueuedAsync(ITrackQueueItem queueItem, int position,
        CancellationToken cancellationToken = new())
    {
        await base.NotifyTrackEnqueuedAsync(queueItem, position, cancellationToken).ConfigureAwait(false);
        await UpdateSocketsAsync(updateQueue: true).ConfigureAwait(false);
    }
}