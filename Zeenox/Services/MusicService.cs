using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Discord;
using Lavalink4NET;
using Lavalink4NET.Artwork;
using Lavalink4NET.Extensions;
using Lavalink4NET.InactivityTracking;
using Lavalink4NET.InactivityTracking.Events;
using Lavalink4NET.Lyrics;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.Tracks;
using Zeenox.Models;

namespace Zeenox.Services;

public class MusicService
{
    private readonly IAudioService _audioService;
    private readonly DatabaseService _databaseService;
    private readonly SpotifyService _spotifyService;
    private readonly IArtworkService _artworkService;
    private readonly ILyricsService _lyricsService;
    private readonly Dictionary<ulong, List<WebSocket>> _webSockets = new();

    public MusicService(
        IAudioService audioService,
        DatabaseService databaseService,
        SpotifyService spotifyService,
        ILyricsService lyricsService,
        IInactivityTrackingService trackingService,
        IArtworkService artworkService
    )
    {
        _databaseService = databaseService;
        _spotifyService = spotifyService;
        _lyricsService = lyricsService;
        _artworkService = artworkService;
        _audioService = audioService;
        trackingService.InactivePlayer += OnInactivePlayer;
    }

    private static async Task OnInactivePlayer(object sender, InactivePlayerEventArgs eventArgs)
    {
        if (!eventArgs.ShouldStop)
            return;
        var player = (ZeenoxPlayer)eventArgs.Player;
        await player.DeleteMessageAsync().ConfigureAwait(false);
        await player.StopAsync(true).ConfigureAwait(false);
    }

    public async Task<(bool playerExists, ZeenoxPlayer? player)> TryGetPlayer(ulong guildId)
    {
        var player = await _audioService.Players
            .GetPlayerAsync<ZeenoxPlayer>(guildId)
            .ConfigureAwait(false);
        return (player is not null, player);
    }

    public async Task<ZeenoxPlayer> GetOrCreatePlayer(
        ulong guildId,
        ITextChannel textChannel,
        IVoiceChannel voiceChannel,
        LavalinkTrack? initialTrack = null
    )
    {
        var (playerExists, player) = await TryGetPlayer(guildId).ConfigureAwait(false);
        if (playerExists)
            return player!;

        var guildConfig = await _databaseService.GetGuildConfigAsync(guildId).ConfigureAwait(false);

        var factory = new PlayerFactory<ZeenoxPlayer, ZeenoxPlayerOptions>(
            (
                (properties, _) =>
                {
                    properties.Options.Value.TextChannel = textChannel;
                    properties.Options.Value.VoiceChannel = voiceChannel;
                    properties.Options.Value.SpotifyService = _spotifyService;
                    properties.Options.Value.ArtworkService = _artworkService;
                    return ValueTask.FromResult(new ZeenoxPlayer(properties));
                }
            )
        );

        player = await _audioService.Players
            .JoinAsync(
                guildId,
                voiceChannel.Id,
                factory,
                options =>
                {
                    options.SelfDeaf = true;
                    options.InitialVolume =
                        (float)Math.Floor(guildConfig.MusicSettings.DefaultVolume / (double)2)
                        / 100f;
                    if (initialTrack is not null)
                    {
                        options.InitialTrack = new TrackReference(initialTrack);
                    }
                }
            )
            .ConfigureAwait(false);
        return player;
    }

    public async Task<bool> PlayAsync(
        ulong guildId,
        ITextChannel textChannel,
        IVoiceChannel voiceChannel,
        IUser requester,
        string query
    )
    {
        var results = await _audioService.Tracks
            .LoadTracksAsync(
                query,
                new TrackLoadOptions
                {
                    SearchMode = Uri.IsWellFormedUriString(query, UriKind.Absolute)
                        ? TrackSearchMode.None
                        : TrackSearchMode.YouTube
                }
            )
            .ConfigureAwait(false);
        if (!results.HasMatches)
            return false;

        if (results.Tracks.Length == 0)
            return false;

        var tracks = results.Tracks;
        var player = await GetOrCreatePlayer(guildId, textChannel, voiceChannel)
            .ConfigureAwait(false);

        /*foreach (var track in tracks)
        {
            track.Context = new TrackContext { Requester = requester };
        }*/

        if (results.IsPlaylist)
        {
            await player.PlayAsync(tracks).ConfigureAwait(false);
            return true;
        }

        await player
            .PlayAsync(new TrackReference(tracks[0]), default(TrackPlayProperties))
            .ConfigureAwait(false);
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
        var favorites = (
            await _databaseService.GetUserAsync(requester.Id).ConfigureAwait(false)
        ).FavoriteSongs;
        if (favorites.Count == 0)
            return;

        var player = await GetOrCreatePlayer(guildId, textChannel, voiceChannel)
            .ConfigureAwait(false);
        var tracks = tracksEnumerable.ToArray();
        /*foreach (var track in tracks)
        {
            track.Context = new TrackContext { Requester = requester };
        }*/

        await player.PlayAsync(tracks).ConfigureAwait(false);
    }

    public async Task SkipAsync(ulong guildId)
    {
        var (playerExists, player) = await TryGetPlayer(guildId).ConfigureAwait(false);
        if (!playerExists)
            return;

        await player!.SkipAsync().ConfigureAwait(false);
    }

    public async Task RewindAsync(ulong guildId)
    {
        var (playerExists, player) = await TryGetPlayer(guildId).ConfigureAwait(false);
        if (!playerExists)
            return;

        await player!.RewindAsync().ConfigureAwait(false);
    }

    public async Task SetVolumeAsync(ulong guildId, int volume)
    {
        var (playerExists, player) = await TryGetPlayer(guildId).ConfigureAwait(false);
        if (!playerExists)
            return;

        await player!
            .SetVolumeAsync((float)Math.Floor(volume / (double)2) / 100f)
            .ConfigureAwait(false);
    }

    public async Task OffsetVolumeAsync(ulong guildId, int offset)
    {
        var (playerExists, player) = await TryGetPlayer(guildId).ConfigureAwait(false);
        if (!playerExists)
            return;

        var volume = (int)(player!.Volume * 100);
        volume += offset;
        await player.SetVolumeAsync(volume / 100f).ConfigureAwait(false);
    }

    public async Task<bool> PauseOrResumeAsync(ulong guildId)
    {
        var (playerExists, player) = await TryGetPlayer(guildId).ConfigureAwait(false);
        if (!playerExists)
            return false;

        if (player!.State is PlayerState.Paused)
        {
            await player.ResumeAsync().ConfigureAwait(false);
        }
        else
        {
            await player.PauseAsync().ConfigureAwait(false);
        }

        return player.State is PlayerState.Paused;
    }

    public async Task CycleLoopMode(ulong guildId)
    {
        var (playerExists, player) = await TryGetPlayer(guildId).ConfigureAwait(false);
        if (!playerExists)
            return;

        var shouldDisable = !Enum.IsDefined(typeof(TrackRepeatMode), player!.RepeatMode + 1);
        await player
            .SetLoopModeAsync(shouldDisable ? 0 : player.RepeatMode + 1)
            .ConfigureAwait(false);
    }

    public async Task SeekAsync(ulong guildId, int position)
    {
        var (playerExists, player) = await TryGetPlayer(guildId).ConfigureAwait(false);
        if (!playerExists)
            return;

        await player!.SeekAsync(TimeSpan.FromSeconds(position)).ConfigureAwait(false);
    }

    public async Task ClearQueueAsync(ulong guildId)
    {
        var (playerExists, player) = await TryGetPlayer(guildId).ConfigureAwait(false);
        if (!playerExists)
            return;

        await player!.ClearQueueAsync().ConfigureAwait(false);
    }

    public async Task ShuffleQueueAsync(ulong guildId)
    {
        var (playerExists, player) = await TryGetPlayer(guildId).ConfigureAwait(false);
        if (!playerExists)
            return;

        await player!.Queue.ShuffleAsync().ConfigureAwait(false);
    }

    public async Task DistinctQueueAsync(ulong guildId)
    {
        var (playerExists, player) = await TryGetPlayer(guildId).ConfigureAwait(false);
        if (!playerExists)
            return;
        await player!.DistinctQueueAsync().ConfigureAwait(false);
    }

    public async Task<string?> GetLyricsAsync(ulong guildId)
    {
        var (playerExists, player) = await TryGetPlayer(guildId).ConfigureAwait(false);
        if (!playerExists)
            return null;

        return player!.CurrentTrack is null
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
        var (playerExists, player) = await TryGetPlayer(guildId).ConfigureAwait(false);
        if (!playerExists)
            return 0;

        if (player!.CurrentTrack is null)
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
        var (playerExists, player) = await TryGetPlayer(guildId).ConfigureAwait(false);
        if (!playerExists)
            return;

        await player!.ReverseQueueAsync().ConfigureAwait(false);
    }

    public async Task StopAsync(ulong guildId)
    {
        var (playerExists, player) = await TryGetPlayer(guildId).ConfigureAwait(false);
        if (!playerExists)
            return;

        await player!.StopAsync().ConfigureAwait(false);
    }
}
