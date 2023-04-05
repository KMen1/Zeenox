using Discord;
using Discord.Interactions;
using Zeenox.Services;

namespace Zeenox.Modules.Music.Preconditions;

public class RequirePlayerAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        var musicService = services.GetRequiredService<MusicService>();
        var playerExists = musicService.TryGetPlayer(context.Guild.Id, out _);

        return Task.FromResult(
            !playerExists
                ? PreconditionResult.FromError("The player does not exist")
                : PreconditionResult.FromSuccess()
        );
    }
}
