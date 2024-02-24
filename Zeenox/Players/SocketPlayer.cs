using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using Discord;
using Discord.WebSocket;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Protocol.Payloads.Events;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.Tracks;
using Zeenox.Dtos;
using Zeenox.Models;
using Zeenox.Models.Actions.Queue;
using Zeenox.Models.Player;
using Zeenox.Models.Socket;
using Zeenox.Services;
using Action = Zeenox.Models.Actions.Action;

namespace Zeenox.Players;

public sealed class SocketPlayer
    (IPlayerProperties<SocketPlayer, SocketPlayerOptions> properties) : LoggedPlayer(properties)
{
    private readonly ConcurrentDictionary<ulong, WebSocket> _webSockets = new();
    private readonly DatabaseService _dbService = properties.Options.Value.DbService;
    private readonly DiscordSocketClient _discordClient = properties.Options.Value.DiscordClient;
    private ResumeSession? ResumeSession { get; set; } = properties.Options.Value.ResumeSession;
    public bool HasResumeSession => ResumeSession is not null;
    private bool IsRunningUpdatePositionLoop { get; set; }

    protected override async Task AddActionAsync(Action action)
    {
        await base.AddActionAsync(action).ConfigureAwait(false);
        await UpdateSocketsAsync(updateActions: true).ConfigureAwait(false);
    }

    protected override async Task<int> PlayAsync(IEnumerable<ExtendedTrackItem> tracksEnumerable)
    {
        var result = await base.PlayAsync(tracksEnumerable).ConfigureAwait(false);
        await UpdateSocketsAsync(updateQueue: true).ConfigureAwait(false);
        return result;
    }

    public override async Task<int> PlayAsync(IUser user, TrackLoadResult trackLoadResult)
    {
        var result = await base.PlayAsync(user, trackLoadResult).ConfigureAwait(false);
        await UpdateSocketsAsync(updateQueue: true).ConfigureAwait(false);
        return result;
    }

    public override async ValueTask<bool> RemoveAsync(IUser user, int index)
    {
        var result = await base.RemoveAsync(user, index).ConfigureAwait(false);
        if (result)
            await UpdateSocketsAsync(updateQueue: true).ConfigureAwait(false);
        return result;
    }

    public override async ValueTask SeekAsync(IUser user, int position)
    {
        await base.SeekAsync(user, position).ConfigureAwait(false);
        await UpdateSocketsAsync(updatePlayer: true).ConfigureAwait(false);
    }

    public override async ValueTask<bool> MoveTrackAsync(int from, int to)
    {
        var result = await base.MoveTrackAsync(from, to).ConfigureAwait(false);
        if (result)
            await UpdateSocketsAsync(updateQueue: true).ConfigureAwait(false);
        return result;
    }

    public override async ValueTask PauseAsync(IUser user)
    {
        await base.PauseAsync(user).ConfigureAwait(false);
        await UpdateSocketsAsync(updatePlayer: true).ConfigureAwait(false);
    }

    public override async ValueTask ResumeAsync(IUser user)
    {
        await base.ResumeAsync(user).ConfigureAwait(false);
        await UpdateSocketsAsync(updatePlayer: true).ConfigureAwait(false);
    }

    public override async ValueTask<bool> MoveTrackAsync(IUser user, int from, int to)
    {
        var result = await base.MoveTrackAsync(user, from, to).ConfigureAwait(false);
        if (result)
            await UpdateSocketsAsync(updateQueue: true).ConfigureAwait(false);
        return result;
    }

    public override async ValueTask SetVolumeAsync(IUser user, int volume)
    {
        await base.SetVolumeAsync(user, volume).ConfigureAwait(false);
        await UpdateSocketsAsync(updatePlayer: true).ConfigureAwait(false);
    }

    public override async Task SetRepeatModeAsync(IUser user, TrackRepeatMode repeatMode)
    {
        await base.SetRepeatModeAsync(user, repeatMode).ConfigureAwait(false);
        await UpdateSocketsAsync(updatePlayer: true).ConfigureAwait(false);
    }

    public override async Task CycleRepeatModeAsync(IUser user)
    {
        await base.CycleRepeatModeAsync(user).ConfigureAwait(false);
        await UpdateSocketsAsync(updatePlayer: true).ConfigureAwait(false);
    }

    public override async ValueTask ShuffleAsync(IUser user)
    {
        await base.ShuffleAsync(user).ConfigureAwait(false);
        await UpdateSocketsAsync(updateQueue: true).ConfigureAwait(false);
    }

    public override async ValueTask<int> DistinctQueueAsync(IUser user)
    {
        var result = await base.DistinctQueueAsync(user).ConfigureAwait(false);
        await UpdateSocketsAsync(updateQueue: true).ConfigureAwait(false);
        return result;
    }

    public override async ValueTask<int> ClearQueueAsync(IUser user)
    {
        var result = await base.ClearQueueAsync(user).ConfigureAwait(false);
        await UpdateSocketsAsync(updateQueue: true).ConfigureAwait(false);
        return result;
    }

    public override async Task ReverseQueueAsync(IUser user)
    {
        await base.ReverseQueueAsync(user).ConfigureAwait(false);
        await UpdateSocketsAsync(updateQueue: true).ConfigureAwait(false);
    }

    public override async Task ToggleAutoPlayAsync(IUser user)
    {
        await base.ToggleAutoPlayAsync(user).ConfigureAwait(false);
        await UpdateSocketsAsync(updatePlayer: true).ConfigureAwait(false);
    }

    protected override async ValueTask<bool> SkipToAsync(int index)
    {
        var result = await base.SkipToAsync(index).ConfigureAwait(false);
        if (result)
            await UpdateSocketsAsync(updateQueue: true).ConfigureAwait(false);
        return result;
    }

    public override async ValueTask<bool> RemoveAtAsync(IUser user, int index)
    {
        var result = await base.RemoveAtAsync(user, index).ConfigureAwait(false);
        if (result)
            await UpdateSocketsAsync(updateQueue: true).ConfigureAwait(false);
        return result;
    }

    public async Task RegisterSocketAsync(ulong userId, WebSocket socket)
    {
        RemoveSocket(userId);
        _webSockets.TryAdd(userId, socket);
        await InitializeSocket(socket).ConfigureAwait(false);
    }

    private async Task InitializeSocket(WebSocket socket)
    {
        var resumeSessionDto = ResumeSession is not null ? new ResumeSessionDTO(ResumeSession, _discordClient) : null;
        var initPlayerPayload = new InitPlayerPayload(this, resumeSessionDto);
        await socket.SendTextAsync(JsonSerializer.Serialize(initPlayerPayload)).ConfigureAwait(false);
        if (!IsRunningUpdatePositionLoop)
            _ = UpdatePositionLoopAsync();
    }
    
    private void RemoveSocket(ulong userId)
    {
        if (!_webSockets.ContainsKey(userId)) return;
        _webSockets.TryRemove(userId, out _);
    }

    private async Task UpdatePositionLoopAsync()
    {
        IsRunningUpdatePositionLoop = true;
        while (!_webSockets.IsEmpty)
        {
            foreach (var (userId, socket) in _webSockets.ToList())
            {
                if (State is PlayerState.Playing && socket.State == WebSocketState.Open)
                {
                    await socket.SendTextAsync(JsonSerializer.Serialize(new UpdatePlayerPayload(this))).ConfigureAwait(false);
                }
                else if (socket.State is WebSocketState.Closed or WebSocketState.Aborted)
                {
                    RemoveSocket(userId);
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
        }
        IsRunningUpdatePositionLoop = false;
    }

    private async Task UpdateSocketsAsync(
        bool updatePlayer = false,
        bool updateTrack = false,
        bool updateQueue = false,
        bool updateActions = false
    )
    {
        if (_webSockets.IsEmpty)
            return;

        foreach (var (_, socket) in _webSockets)
        {
            if (socket.State != WebSocketState.Open)
                continue;
            await socket.SendSocketMessagesAsync(this, updatePlayer, updateTrack, updateQueue, updateActions).ConfigureAwait(false);
        }
    }

    public async Task ResumeSessionAsync(IUser user)
    {
        if (ResumeSession is null)
            return;
        
        var newRequester = _discordClient.GetUser(ResumeSession.CurrentTrack.RequesterId.GetValueOrDefault());
        var newCurrentTrack = new ExtendedTrackItem(LavalinkTrack.Parse(ResumeSession.CurrentTrack.Id, null), newRequester);
        var queue = ResumeSession.Queue.Select(x => new ExtendedTrackItem(LavalinkTrack.Parse(x.Id, null), newRequester)).ToList();

        if (queue.Count > 0)
        {
            await Queue.AddRangeAsync(queue).ConfigureAwait(false);
            await AddActionAsync(new EnqueuePlaylistAction(user, null, queue)).ConfigureAwait(false);
        }
        await PlayAsync(user, newCurrentTrack, false).ConfigureAwait(false);
        await _dbService.DeleteResumeSessionAsync(GuildId).ConfigureAwait(false);
        ResumeSession = null;
    }
    
    public void RemoveResumeSession()
    {
        ResumeSession = null;
    }
    
    protected override async ValueTask DisposeAsyncCore()
    {
        foreach (var (_, socket) in _webSockets)
        {
            if (socket.State != WebSocketState.Open)
                continue;
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disposed", CancellationToken.None).ConfigureAwait(false);
            socket.Dispose();
        }
        
        if (Queue.Count > 0)
        {
            var resumeSession = new ResumeSession(this);
            await _dbService.SaveResumeSessionAsync(resumeSession).ConfigureAwait(false);
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