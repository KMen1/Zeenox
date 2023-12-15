using Lavalink4NET;
using Lavalink4NET.InactivityTracking;
using Lavalink4NET.InactivityTracking.Events;
using Lavalink4NET.Lyrics;
using Lavalink4NET.Players;
using Zeenox.Players;

namespace Zeenox.Services;

public class MusicService
{
    private readonly IAudioService _audioService;
    private readonly ILyricsService _lyricsService;

    public MusicService(
        IAudioService audioService,
        ILyricsService lyricsService,
        IInactivityTrackingService trackingService
    )
    {
        _lyricsService = lyricsService;
        _audioService = audioService;
        trackingService.PlayerInactive += OnInactivePlayerAsync;
    }

    private static async Task OnInactivePlayerAsync(
        object sender,
        PlayerInactiveEventArgs eventArgs
    )
    {
        var player = (LoggedPlayer)eventArgs.Player;
        await player.DeleteNowPlayingMessageAsync().ConfigureAwait(false);
    }

    private async Task<T?> TryGetPlayerAsync<T>(ulong guildId)
        where T : class, ILavalinkPlayer
    {
        return await _audioService.Players.GetPlayerAsync<T>(guildId).ConfigureAwait(false);
    }

    public Task<LoggedPlayer?> TryGetPlayerAsync(ulong guildId)
    {
        return TryGetPlayerAsync<LoggedPlayer>(guildId);
    }

    public async Task<string?> GetLyricsAsync(ulong guildId)
    {
        var player = await TryGetPlayerAsync(guildId).ConfigureAwait(false);

        return player?.CurrentTrack is null
            ? null
            : await _lyricsService.GetLyricsAsync(player.CurrentTrack).ConfigureAwait(false);
    }
}
