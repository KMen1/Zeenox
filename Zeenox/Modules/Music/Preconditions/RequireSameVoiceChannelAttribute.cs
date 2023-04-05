using Discord;
using Discord.Interactions;
using Zeenox.Services;

namespace Zeenox.Modules.Music.Preconditions;

public class RequireSameVoiceChannelAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        var user = context.User;
        var voiceState = user as IVoiceState;

        var musicService = services.GetRequiredService<MusicService>();
        var playerExists = musicService.TryGetPlayer(context.Guild.Id, out var player);

        if (playerExists && player?.VoiceChannelId != voiceState?.VoiceChannel.Id)
            return Task.FromResult(
                PreconditionResult.FromError("You must be in the same voice channel as the bot")
            );

        return Task.FromResult(PreconditionResult.FromSuccess());
    }
}
