using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Discord;
using Lavalink4NET;
using Lavalink4NET.Decoding;
using Lavalink4NET.Player;
using Lavalink4NET.Rest;
using Lavalink4NET.Tracking;
using Zeenox.Models;

namespace Zeenox.Services;

public class MusicService
{
    private readonly LavalinkNode _lavaNode;
    private readonly DatabaseService _databaseService;
    private readonly Dictionary<ulong, List<WebSocket>> _webSockets = new();

    public MusicService(
        IAudioService audioService,
        InactivityTrackingService trackingService,
        DatabaseService databaseService
    )
    {
        _databaseService = databaseService;
        _lavaNode = (LavalinkNode)audioService;
    }

    public bool TryGetPlayer(ulong guildId, out ZeenoxPlayer? player)
    {
        player = _lavaNode.GetPlayer<ZeenoxPlayer>(guildId);
        return player is not null;
    }

    public async Task<ZeenoxPlayer> GetOrCreatePlayer(
        ulong guildId,
        ITextChannel textChannel,
        IVoiceChannel voiceChannel
    )
    {
        if (TryGetPlayer(guildId, out var player))
            return player!;

        var guildConfig = await _databaseService.GetGuildConfigAsync(guildId);

        player = await _lavaNode.JoinAsync(
            () => new ZeenoxPlayer(textChannel, voiceChannel),
            guildId,
            voiceChannel.Id,
            true
        );
        await player.SetVolumeAsync(guildConfig.MusicSettings.DefaultVolume / 100f);
        return player;
    }

    public async Task<bool> PlayAsync(
        ulong guildId,
        ITextChannel textChannel,
        IVoiceChannel voiceChannel,
        IUser requester,
        string query,
        bool force = false
    )
    {
        var results = await _lavaNode.LoadTracksAsync(query);
        if (results.LoadType is TrackLoadType.NoMatches)
            return false;

        if (results.Tracks is null || results.Tracks.Length == 0)
            return false;

        var player = await GetOrCreatePlayer(guildId, textChannel, voiceChannel);
        var tracks = results.Tracks;
        foreach (var track in tracks)
        {
            track.Context = new TrackContext { Requester = requester };
        }

        if (results.LoadType is TrackLoadType.PlaylistLoaded)
        {
            await player.PlayAsync(tracks);
            return true;
        }

        await player.PlayAsync(tracks[0], force);
        return true;
    }

    public async Task PlayAsync(
        ulong guildId,
        ITextChannel textChannel,
        IVoiceChannel voiceChannel,
        IUser requester,
        IEnumerable<LavalinkTrack> tracksEnumerable
    )
    {
        var favorites = (await _databaseService.GetUserAsync(requester.Id)).FavoriteSongs;
        if (favorites.Count == 0)
            return;

        var player = await GetOrCreatePlayer(guildId, textChannel, voiceChannel);
        var tracks = tracksEnumerable.ToArray();
        foreach (var track in tracks)
        {
            track.Context = new TrackContext { Requester = requester };
        }

        await player.PlayAsync(tracks);
    }

    public async Task SkipAsync(ulong guildId)
    {
        if (!TryGetPlayer(guildId, out var player))
            return;

        await player!.SkipAsync();
    }

    public async Task RewindAsync(ulong guildId)
    {
        if (!TryGetPlayer(guildId, out var player))
            return;

        await player!.RewindAsync();
    }

    public async Task SetVolumeAsync(ulong guildId, int volume)
    {
        if (!TryGetPlayer(guildId, out var player))
            return;

        await player!.SetVolumeAsync(volume / 100f);
    }

    public async Task SetVolumeAsync(ulong guildId, Action<int> volumeAction)
    {
        if (!TryGetPlayer(guildId, out var player))
            return;

        var volume = (int)(player!.Volume * 100);
        volumeAction(volume);
        await player.SetVolumeAsync(volume / 100f);
    }

    public async Task<bool> PauseOrResumeAsync(ulong guildId)
    {
        if (!TryGetPlayer(guildId, out var player))
            return false;

        if (player!.State is PlayerState.Paused)
        {
            await player.ResumeAsync();
        }
        else
        {
            await player.PauseAsync();
        }

        return player.State is PlayerState.Paused;
    }

    public Task CycleLoopMode(ulong guildId)
    {
        if (!TryGetPlayer(guildId, out var player))
            return Task.CompletedTask;

        var shouldDisable = !Enum.IsDefined(typeof(PlayerLoopMode), player!.LoopMode + 1);
        return player.SetLoopModeAsync(shouldDisable ? 0 : player.LoopMode + 1);
    }

    public Task SeekAsync(ulong guildId, int position)
    {
        return !TryGetPlayer(guildId, out var player)
            ? Task.CompletedTask
            : player!.SeekPositionAsync(TimeSpan.FromSeconds(position));
    }

    public Task ClearQueueAsync(ulong guildId)
    {
        return !TryGetPlayer(guildId, out var player)
            ? Task.CompletedTask
            : player!.ClearQueueAsync();
    }

    public Task ShuffleQueueAsync(ulong guildId)
    {
        if (!TryGetPlayer(guildId, out var player))
            return Task.CompletedTask;

        player!.Queue.Shuffle();
        return Task.CompletedTask;
    }

    public Task DistinctQueueAsync(ulong guildId)
    {
        return !TryGetPlayer(guildId, out var player)
            ? Task.CompletedTask
            : player!.DistinctQueueAsync();
    }

    public async Task SendWebSocketMessagesAsync(ulong guildId, SocketMessage message)
    {
        if (!_webSockets.ContainsKey(guildId))
            return;

        var webSockets = _webSockets[guildId];
        foreach (var webSocket in webSockets)
        {
            await webSocket.SendAsync(
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }
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
}
