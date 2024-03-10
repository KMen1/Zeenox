using System.Reflection;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.Interactions;
using Discord.WebSocket;
using IResult = Discord.Interactions.IResult;

namespace Zeenox.Services;

public sealed class InteractionHandler(
    DiscordSocketClient client,
    ILogger<DiscordClientService> logger,
    InteractionService interactionService,
    IServiceProvider provider,
    IConfiguration config)
    : DiscordClientService(client, logger)
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.InteractionCreated += HandleInteractionAsync;
        interactionService.SlashCommandExecuted += HandleSlashCommandResultAsync;
        interactionService.ComponentCommandExecuted += HandleComponentCommandResultAsync;

        await Client.WaitForReadyAsync(stoppingToken).ConfigureAwait(false);
        await Client
              .SetGameAsync(
                  config["Discord:Activity"] ?? throw new Exception("Discord activity not set!"),
                  type: ActivityType.Listening
              )
              .ConfigureAwait(false);
        await interactionService
              .AddModulesAsync(Assembly.GetEntryAssembly(), provider)
              .ConfigureAwait(false);
        await interactionService
              .AddModulesGloballyAsync(true, interactionService.Modules.ToArray())
              .ConfigureAwait(false);
    }

    private static Task HandleComponentCommandResultAsync(
        ComponentCommandInfo componentInfo,
        IInteractionContext context,
        IResult result
    )
    {
        if (result.IsSuccess || result.Error == InteractionCommandError.UnknownCommand)
        {
            return Task.CompletedTask;
        }

        var reason = GetErrorReason(result);
        return SendErrorMessageAsync(reason, context.Interaction);
    }

    private static Task HandleSlashCommandResultAsync(
        SlashCommandInfo commandInfo,
        IInteractionContext context,
        IResult result
    )
    {
        if (result.IsSuccess || result.Error == InteractionCommandError.UnknownCommand)
        {
            return Task.CompletedTask;
        }

        var reason = GetErrorReason(result);
        return SendErrorMessageAsync(reason, context.Interaction, result.ErrorReason);
    }

    private Task HandleInteractionAsync(SocketInteraction interaction)
    {
        var ctx = new SocketInteractionContext(Client, interaction);
        return interactionService.ExecuteCommandAsync(ctx, provider);
    }

    private static string GetErrorReason(IResult result)
    {
        return result.Error switch
        {
            InteractionCommandError.UnmetPrecondition => result.ErrorReason,
            InteractionCommandError.UnknownCommand
                => "Unknown command, please restart your discord client!",
            _ => "Something went wrong, please try again!"
        };
    }

    private static Task SendErrorMessageAsync(
        string reason,
        IDiscordInteraction interaction,
        string? description = null
    )
    {
        var embed = new EmbedBuilder().WithColor(Color.Red).WithTitle(reason);
        if (description is not null)
        {
            embed.WithDescription($"```{description}```");
        }

        return interaction.HasResponded
            ? interaction.FollowupAsync(embed: embed.Build(), ephemeral: true)
            : interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
    }
}