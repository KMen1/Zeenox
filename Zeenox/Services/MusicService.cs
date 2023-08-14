using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Lavalink4NET;
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
    private readonly Dictionary<ulong, List<WebSocket>> _webSockets = new();

    public MusicService(
        IAudioService audioService,
        ILyricsService lyricsService,
        IInactivityTrackingService trackingService
    )
    {
        _lyricsService = lyricsService;
        _audioService = audioService;
        trackingService.InactivePlayer += OnInactivePlayer;
    }

    private static async Task OnInactivePlayer(object sender, InactivePlayerEventArgs eventArgs)
    {
        if (!eventArgs.ShouldStop)
            return;
        var player = (ZeenoxPlayer)eventArgs.Player;
        await player.DisposeAsync().ConfigureAwait(false);
    }

    public async Task<ZeenoxPlayer?> TryGetPlayerAsync(ulong guildId)
    {
        var player = await _audioService.Players
            .GetPlayerAsync<ZeenoxPlayer>(guildId)
            .ConfigureAwait(false);
        return player;
    }

    public async Task SkipAsync(ulong guildId)
    {
        var player = await TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
            return;

        await player.SkipAsync().ConfigureAwait(false);
    }

    public async Task RewindAsync(ulong guildId)
    {
        var player = await TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
            return;

        await player.RewindAsync().ConfigureAwait(false);
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

    public async Task OffsetVolumeAsync(ulong guildId, int offset)
    {
        var player = await TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
            return;

        var volume = (int)(player.Volume * 100);
        volume += offset;
        await player.SetVolumeAsync(volume / 100f).ConfigureAwait(false);
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

    public async Task CycleLoopMode(ulong guildId)
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

    public async Task ClearQueueAsync(ulong guildId)
    {
        var player = await TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
            return;

        await player.ClearQueueAsync().ConfigureAwait(false);
    }

    public async Task ShuffleQueueAsync(ulong guildId)
    {
        var player = await TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
            return;

        await player.Queue.ShuffleAsync().ConfigureAwait(false);
    }

    public async Task DistinctQueueAsync(ulong guildId)
    {
        var player = await TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
            return;
        await player.DistinctQueueAsync().ConfigureAwait(false);
    }

    public async Task<string?> GetLyricsAsync(ulong guildId)
    {
        var player = await TryGetPlayerAsync(guildId).ConfigureAwait(false);

        return player?.CurrentTrack is null
            ? null
            : await _lyricsService.GetLyricsAsync(player.CurrentTrack).ConfigureAwait(false);
    }

    public async Task SendWebSocketMessagesAsync(ulong guildId, SocketMessage message)
    {
        if (!_webSockets.ContainsKey(guildId))
            return;

        var webSockets = _webSockets[guildId];
        foreach (var webSocket in webSockets)
        {
            await webSocket
                .SendAsync(
                    Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)),
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

        return (int)player.Position!.Value.Position.TotalSeconds;
    }

    public void AddWebSocket(ulong guildId, WebSocket webSocket)
    {
        if (!_webSockets.ContainsKey(guildId))
            _webSockets.Add(guildId, new List<WebSocket>());

        _webSockets[guildId].Add(webSocket);
    }

    public void RemoveWebSocket(ulong guildId, WebSocket webSocket)
    {
        if (!_webSockets.ContainsKey(guildId))
            return;

        _webSockets[guildId].Remove(webSocket);
    }

    public async Task ReverseQueueAsync(ulong guildId)
    {
        var player = await TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
            return;

        await player.ReverseQueueAsync().ConfigureAwait(false);
    }

    public async Task StopAsync(ulong guildId)
    {
        var player = await TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
            return;

        await player.StopAsync().ConfigureAwait(false);
    }
}
