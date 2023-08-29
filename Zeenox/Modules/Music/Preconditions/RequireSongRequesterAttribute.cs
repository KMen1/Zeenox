using Discord;
using Discord.Interactions;
using Zeenox.Models;
using Zeenox.Services;

namespace Zeenox.Modules.Music.Preconditions;

public class RequireSongRequesterAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        var musicService = services.GetRequiredService<MusicService>();
        var player = await musicService.TryGetPlayerAsync(context.Guild.Id).ConfigureAwait(false);

        if (
            player is not null
            && (player.CurrentItem as ZeenoxTrackItem)?.RequestedBy.Id != context.User.Id
        )
        {
            return PreconditionResult.FromError(
                "You don't have permission to perfrom this action."
            );
        }

        return PreconditionResult.FromSuccess();
    }
}
