using Discord;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Clients;
using Lavalink4NET.Players;
using Microsoft.Extensions.Options;
using Zeenox.Players;

namespace Zeenox.Services;

public class MusicService(
    IAudioService audioService,
    DatabaseService databaseService,
    DiscordSocketClient client)
{
    private async Task<T?> TryGetPlayerAsync<T>(ulong guildId)
        where T : class, ILavalinkPlayer =>
        await audioService.Players.GetPlayerAsync<T>(guildId).ConfigureAwait(false);

    public Task<SocketPlayer?> TryGetPlayerAsync(ulong guildId) => TryGetPlayerAsync<SocketPlayer>(guildId);

    public async ValueTask<SocketPlayer?> TryCreatePlayerAsync(
        ulong guildId,
        SocketVoiceChannel voiceChannel,
        ITextChannel? textChannel = null
    )
    {
        var resumeSession = await databaseService
                                  .GetResumeSessionAsync(guildId)
                                  .ConfigureAwait(false);
        var factory = new PlayerFactory<SocketPlayer, SocketPlayerOptions>(
            (properties, _) =>
            {
                properties.Options.Value.TextChannel = textChannel;
                properties.Options.Value.VoiceChannel = voiceChannel;
                properties.Options.Value.DbService = databaseService;
                properties.Options.Value.DiscordClient = client;
                properties.Options.Value.ResumeSession = resumeSession;
                properties.Options.Value.AudioService = audioService;
                return ValueTask.FromResult(new SocketPlayer(properties));
            }
        );

        var retrieveOptions = new PlayerRetrieveOptions(
            PlayerChannelBehavior.Join,
            MemberVoiceStateBehavior.RequireSame
        );

        var guildConfig = await databaseService.GetGuildConfigAsync(guildId).ConfigureAwait(false);

        var result = await audioService.Players
                                       .RetrieveAsync(
                                           guildId,
                                           voiceChannel.Id,
                                           factory,
                                           new OptionsWrapper<SocketPlayerOptions>(
                                               new SocketPlayerOptions
                                               {
                                                   InitialVolume =
                                                       (float)Math.Floor(
                                                           guildConfig.MusicSettings.DefaultVolume / (double)2)
                                                       / 100f,
                                                   ClearQueueOnStop = false,
                                                   ClearHistoryOnStop = false,
                                                   HistoryCapacity = 100,
                                               }
                                           ),
                                           retrieveOptions
                                       )
                                       .ConfigureAwait(false);

        return result.IsSuccess ? result.Player : null;
    }
}