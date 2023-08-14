using Discord;
using Discord.Interactions;
using Zeenox.Services;

namespace Zeenox.Modules.Music.Preconditions;

public class MustBeSongRequesterAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        /*var musicService = services.GetRequiredService<MusicService>();
        var player = await musicService.TryGetPlayerAsync(context.Guild.Id).ConfigureAwait(false);

        if (
            playerExists
            && ((TrackContext)player?.CurrentTrack?.Context!)?.Requester.Id != context.User.Id
        )
            return Task.FromResult(
                PreconditionResult.FromError("You must be the requester of the song")
            );*/

        return PreconditionResult.FromSuccess();
    }
}
