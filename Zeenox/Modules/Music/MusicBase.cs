using System.Collections.Immutable;
using Discord;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Clients;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Preconditions;
using Microsoft.Extensions.Options;
using Zeenox.Players;
using Zeenox.Services;

namespace Zeenox.Modules.Music;

public class MusicBase : ModuleBase
{
    public IAudioService AudioService { get; set; } = null!;
    public DatabaseService DatabaseService { get; set; } = null!;

    protected async ValueTask<SocketPlayer?> TryGetPlayerAsync(
        bool allowConnect = false,
        bool requireChannel = true,
        ImmutableArray<IPlayerPrecondition> preconditions = default,
        bool isDeferred = false,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var voiceState = Context.User as IVoiceState;
        var resumeSession = await DatabaseService
            .GetResumeSessionAsync(Context.Guild.Id)
            .ConfigureAwait(false);
        var factory = new PlayerFactory<SocketPlayer, SocketPlayerOptions>(
            (properties, _) =>
            {
                properties.Options.Value.TextChannel = (ITextChannel)Context.Channel;
                properties.Options.Value.VoiceChannel = (
                    voiceState!.VoiceChannel as SocketVoiceChannel
                )!;
                properties.Options.Value.DbService = DatabaseService;
                properties.Options.Value.ResumeSession = resumeSession;
                properties.Options.Value.DiscordClient = Context.Client;
                properties.Options.Value.AudioService = AudioService;
                return ValueTask.FromResult(new SocketPlayer(properties));
            }
        );

        var retrieveOptions = new PlayerRetrieveOptions(
            allowConnect ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None,
            requireChannel ? MemberVoiceStateBehavior.RequireSame : MemberVoiceStateBehavior.Ignore,
            preconditions
        );

        var guildConfig = await DatabaseService
            .GetGuildConfigAsync(Context.Guild.Id)
            .ConfigureAwait(false);

        var result = await AudioService.Players
            .RetrieveAsync(
                Context.Guild.Id,
                voiceState!.VoiceChannel?.Id,
                factory,
                new OptionsWrapper<SocketPlayerOptions>(
                    new SocketPlayerOptions
                    {
                        InitialVolume =
                            (float)Math.Floor(guildConfig.MusicSettings.DefaultVolume / (double)2)
                            / 100f,
                        ClearQueueOnStop = false,
                        ClearHistoryOnStop = false,
                        HistoryCapacity = 100
                    }
                ),
                retrieveOptions,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return result.Player;
        }

        var errorMessage = CreateErrorEmbed(result);

        if (isDeferred)
        {
            await FollowupAsync(embed: errorMessage).ConfigureAwait(false);
        }
        else
        {
            await RespondAsync(embed: errorMessage).ConfigureAwait(false);
        }

        return null;
    }

    private static Embed CreateErrorEmbed(PlayerResult<SocketPlayer> result)
    {
        var title = result.Status switch
        {
            PlayerRetrieveStatus.UserNotInVoiceChannel => "You must be in a voice channel.",
            PlayerRetrieveStatus.BotNotConnected => "The bot is not connected to any channel.",
            PlayerRetrieveStatus.VoiceChannelMismatch
                => "You must be in the same voice channel as the bot.",
            PlayerRetrieveStatus.PreconditionFailed
                when Equals(result.Precondition, PlayerPrecondition.Playing)
                => "The player is currently now playing any track.",
            PlayerRetrieveStatus.PreconditionFailed
                when Equals(result.Precondition, PlayerPrecondition.NotPaused)
                => "The player is already paused.",
            PlayerRetrieveStatus.PreconditionFailed
                when Equals(result.Precondition, PlayerPrecondition.Paused)
                => "The player is not paused.",
            PlayerRetrieveStatus.PreconditionFailed
                when Equals(result.Precondition, PlayerPrecondition.QueueEmpty)
                => "The queue is empty.",

            _ => "Unknown error."
        };

        return new EmbedBuilder().WithTitle(title).WithColor(Color.Red).Build();
    }
}
