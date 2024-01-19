using Discord;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Clients;
using Lavalink4NET.InactivityTracking;
using Lavalink4NET.InactivityTracking.Events;
using Lavalink4NET.Lyrics;
using Lavalink4NET.Players;
using Microsoft.Extensions.Options;
using Zeenox.Players;

namespace Zeenox.Services;

public class MusicService
{
    private readonly IAudioService _audioService;
    private readonly DatabaseService _databaseService;
    private readonly ILyricsService _lyricsService;

    public MusicService(
        IAudioService audioService,
        ILyricsService lyricsService,
        IInactivityTrackingService trackingService,
        DatabaseService databaseService
    )
    {
        _lyricsService = lyricsService;
        _databaseService = databaseService;
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

    public async ValueTask<LoggedPlayer?> TryCreatePlayerAsync(
        ulong guildId,
        SocketVoiceChannel voiceChannel,
        ITextChannel? textChannel = null
    )
    {
        var factory = new PlayerFactory<LoggedPlayer, InteractivePlayerOptions>(
            (properties, _) =>
            {
                properties.Options.Value.TextChannel = textChannel;
                properties.Options.Value.VoiceChannel = voiceChannel;
                properties.Options.Value.DbService = _databaseService;
                return ValueTask.FromResult(new LoggedPlayer(properties));
            }
        );

        var retrieveOptions = new PlayerRetrieveOptions(
            ChannelBehavior: PlayerChannelBehavior.Join,
            VoiceStateBehavior: MemberVoiceStateBehavior.RequireSame
        );

        var guildConfig = await _databaseService.GetGuildConfigAsync(guildId).ConfigureAwait(false);

        var result = await _audioService.Players
            .RetrieveAsync(
                guildId,
                voiceChannel.Id,
                playerFactory: factory,
                options: new OptionsWrapper<InteractivePlayerOptions>(
                    new InteractivePlayerOptions
                    {
                        SelfDeaf = true,
                        InitialVolume =
                            (float)Math.Floor(guildConfig.MusicSettings.DefaultVolume / (double)2)
                            / 100f,
                    }
                ),
                retrieveOptions
            )
            .ConfigureAwait(false);

        return result.IsSuccess ? result.Player : null;
    }

    public async Task<string?> GetLyricsAsync(ulong guildId)
    {
        var player = await TryGetPlayerAsync(guildId).ConfigureAwait(false);

        return player?.CurrentTrack is null
            ? null
            : await _lyricsService.GetLyricsAsync(player.CurrentTrack).ConfigureAwait(false);
    }
}
