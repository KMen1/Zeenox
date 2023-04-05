using Discord;
using Discord.Interactions;

namespace Zeenox.Modules.Music.Preconditions;

public class RequireVoiceChannelAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        var user = context.User;
        var voiceState = user as IVoiceState;

        return Task.FromResult(
            voiceState?.VoiceChannel is null
                ? PreconditionResult.FromError("You must be in a voice channel to use this command")
                : PreconditionResult.FromSuccess()
        );
    }
}
