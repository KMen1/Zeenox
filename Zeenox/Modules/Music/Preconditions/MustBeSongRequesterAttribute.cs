using Discord;
using Discord.Interactions;
using Zeenox.Models;
using Zeenox.Services;

namespace Zeenox.Modules.Music.Preconditions;

public class MustBeSongRequesterAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        var musicService = services.GetRequiredService<MusicService>();
        var playerExists = musicService.TryGetPlayer(context.Guild.Id, out var player);

        if (
            playerExists
            && ((TrackContext)player?.CurrentTrack?.Context!)?.Requester.Id != context.User.Id
        )
            return Task.FromResult(
                PreconditionResult.FromError("You must be the requester of the song")
            );

        return Task.FromResult(PreconditionResult.FromSuccess());
    }
}
