using Discord;
using Discord.Interactions;
using Zeenox.Services;

namespace Zeenox.Modules.Music.Preconditions;

public class RequireWhitelistedRoleAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        var databaseService = services.GetRequiredService<DatabaseService>();
        var allowedRoles = (
            await databaseService.GetGuildConfigAsync(context.Guild.Id).ConfigureAwait(false)
        )
            .MusicSettings
            .WhiteListRoles;

        if (allowedRoles.Count == 0)
            return PreconditionResult.FromSuccess();

        return (context.User as IGuildUser)!.RoleIds.Any(x => allowedRoles.Contains(x))
            ? PreconditionResult.FromSuccess()
            : PreconditionResult.FromError("You don't have permission to perfrom this action.");
    }
}
