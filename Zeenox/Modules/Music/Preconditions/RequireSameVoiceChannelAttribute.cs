using Discord;
using Discord.Interactions;
using Zeenox.Services;

namespace Zeenox.Modules.Music.Preconditions;

public class RequireSameVoiceChannelAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        var user = context.User;
        var voiceState = user as IVoiceState;

        var musicService = services.GetRequiredService<MusicService>();
        var (playerExists, player) = await musicService.TryGetPlayer(context.Guild.Id).ConfigureAwait(false);

        if (playerExists && player?.VoiceChannelId != voiceState?.VoiceChannel.Id)
            return PreconditionResult.FromError("You must be in the same voice channel as the bot");

        return PreconditionResult.FromSuccess();
    }
}
