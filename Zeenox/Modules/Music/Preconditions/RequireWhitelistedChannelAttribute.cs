using Discord;
using Discord.Interactions;
using Zeenox.Services;

namespace Zeenox.Modules.Music.Preconditions;

public class RequireWhitelistedChannelAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        var databaseService = services.GetRequiredService<DatabaseService>();
        var allowedChannels = (
            await databaseService.GetGuildConfigAsync(context.Guild.Id).ConfigureAwait(false)
        )
            .MusicSettings
            .WhitelistChannels;

        if (allowedChannels.Count == 0)
            return PreconditionResult.FromSuccess();

        return allowedChannels.Contains(context.Channel.Id)
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("This channel is not whitelisted for music commands.");
    }
}
