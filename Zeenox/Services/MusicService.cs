using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Lavalink4NET;
using Lavalink4NET.Events.Players;
using Lavalink4NET.InactivityTracking;
using Lavalink4NET.InactivityTracking.Events;
using Lavalink4NET.Lyrics;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using SocketMessage = Zeenox.Models.SocketMessage;

namespace Zeenox.Services;

public class MusicService
{
    private readonly IAudioService _audioService;
    private readonly ILyricsService _lyricsService;
    private readonly ConcurrentDictionary<ulong, List<WebSocket>> _webSockets = new();

    public MusicService(
        IAudioService audioService,
        ILyricsService lyricsService,
        IInactivityTrackingService trackingService
    )
    {
        _lyricsService = lyricsService;
        _audioService = audioService;
        audioService.TrackStarted += OnTrackStartedAsync;
        audioService.TrackEnded += OnTrackEndedAsync;
        trackingService.PlayerInactive += OnInactivePlayerAsync;
    }

    private Task OnTrackEndedAsync(object sender, TrackEndedEventArgs eventargs)
    {
        return ((ZeenoxPlayer)eventargs.Player).Queue.Count == 0
            ? UpdateSocketsAsync(eventargs.Player.GuildId, true, true, true)
            : Task.CompletedTask;
    }

    private Task OnTrackStartedAsync(object _, TrackStartedEventArgs eventargs)
    {
        return UpdateSocketsAsync(eventargs.Player.GuildId, updateTrack: true, updateQueue: true);
    }

    private static async Task OnInactivePlayerAsync(
        object sender,
        PlayerInactiveEventArgs eventArgs
    )
    {
        var player = (ZeenoxPlayer)eventArgs.Player;
        await player.DeleteNowPlayingMessageAsync().ConfigureAwait(false);
    }

    public async Task<ZeenoxPlayer?> TryGetPlayerAsync(ulong guildId)
    {
        var player = await _audioService.Players
            .GetPlayerAsync<ZeenoxPlayer>(guildId)
            .ConfigureAwait(false);
        return player;
    }

    public async Task SetVolumeAsync(ulong guildId, int volume)
    {
        var player = await TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
            return;

        await player
            .SetVolumeAsync((float)Math.Floor(volume / (double)2) / 100f)
            .ConfigureAwait(false);
    }

    public async Task PauseOrResumeAsync(ulong guildId)
    {
        var player = await TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
            return;

        if (player.State is PlayerState.Paused)
        {
            await player.ResumeAsync().ConfigureAwait(false);
        }
        else
        {
            await player.PauseAsync().ConfigureAwait(false);
        }
    }

    public async Task CycleLoopModeAsync(ulong guildId)
    {
        var player = await TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
            return;

        var shouldDisable = !Enum.IsDefined(typeof(TrackRepeatMode), player.RepeatMode + 1);
        await player
            .SetLoopModeAsync(shouldDisable ? 0 : player.RepeatMode + 1)
            .ConfigureAwait(false);
    }

    public async Task SeekAsync(ulong guildId, int position)
    {
        var player = await TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
            return;

        await player.SeekAsync(TimeSpan.FromSeconds(position)).ConfigureAwait(false);
    }

    public async Task<string?> GetLyricsAsync(ulong guildId)
    {
        var player = await TryGetPlayerAsync(guildId).ConfigureAwait(false);

        return player?.CurrentTrack is null
            ? null
            : await _lyricsService.GetLyricsAsync(player.CurrentTrack).ConfigureAwait(false);
    }

    private async Task UpdateSocketsAsync(ulong guildId, bool updatePlayer = false, bool updateTrack = false, bool updateQueue = false)
    {
        if (!_webSockets.ContainsKey(guildId))
            return;
        var player = await TryGetPlayerAsync(guildId).ConfigureAwait(false);

        var webSockets = _webSockets[guildId];
        webSockets.RemoveAll(x => x.State != WebSocketState.Open);
        foreach (var webSocket in webSockets)
        {
            await webSocket
                .SendAsync(
                    Encoding.UTF8.GetBytes(
                        JsonSerializer.Serialize(
                            player is null
                                ? SocketMessage.Empty
                                : SocketMessage.FromZeenoxPlayer(player, updateQueue, updateTrack, updatePlayer)
                        )
                    ),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                )
                .ConfigureAwait(false);
        }
    }

    public async Task<int> GetPlayerPositionAsync(ulong guildId)
    {
        var player = await TryGetPlayerAsync(guildId).ConfigureAwait(false);

        if (player?.CurrentTrack is null)
            return 0;

        return (int)player.Position?.Position.TotalSeconds!;
    }

    public void AddWebSocket(ulong guildId, WebSocket webSocket)
    {
        if (!_webSockets.ContainsKey(guildId))
            _webSockets.TryAdd(guildId, new List<WebSocket>());

        _webSockets[guildId].Add(webSocket);
    }

    public void RemoveWebSocket(ulong guildId, WebSocket webSocket)
    {
        if (!_webSockets.ContainsKey(guildId))
            return;

        _webSockets[guildId].Remove(webSocket);
    }
}
