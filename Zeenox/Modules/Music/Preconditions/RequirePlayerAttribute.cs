using Discord;
using Discord.Interactions;
using Zeenox.Services;

namespace Zeenox.Modules.Music.Preconditions;

public class RequirePlayerAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        var musicService = services.GetRequiredService<MusicService>();
        var (playerExists, _) = await musicService.TryGetPlayer(context.Guild.Id).ConfigureAwait(false);

        return !playerExists
            ? PreconditionResult.FromError("The player does not exist")
            : PreconditionResult.FromSuccess();
    }
}
