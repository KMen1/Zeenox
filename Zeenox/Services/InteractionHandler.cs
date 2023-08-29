using System.Reflection;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.Interactions;
using Discord.WebSocket;
using IResult = Discord.Interactions.IResult;

namespace Zeenox.Services;

public sealed class InteractionHandler : DiscordClientService
{
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _provider;
    private readonly IConfiguration _config;

    public InteractionHandler(
        DiscordSocketClient client,
        ILogger<DiscordClientService> logger,
        InteractionService interactionService,
        IServiceProvider provider,
        IConfiguration config
    )
        : base(client, logger)
    {
        _interactionService = interactionService;
        _provider = provider;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.InteractionCreated += HandleInteractionAsync;
        _interactionService.SlashCommandExecuted += HandleSlashCommandResultAsync;
        _interactionService.ComponentCommandExecuted += HandleComponentCommandResultAsync;

        await Client.WaitForReadyAsync(stoppingToken).ConfigureAwait(false);
        await Client
            .SetGameAsync(_config.GetSection("Discord")["Activity"], type: ActivityType.Listening)
            .ConfigureAwait(false);
        await _interactionService
            .AddModulesAsync(Assembly.GetEntryAssembly(), _provider)
            .ConfigureAwait(false);
        await _interactionService
            .AddModulesGloballyAsync(true, _interactionService.Modules.ToArray())
            .ConfigureAwait(false);
    }

    private static Task HandleComponentCommandResultAsync(
        ComponentCommandInfo componentInfo,
        IInteractionContext context,
        IResult result
    )
    {
        if (result.IsSuccess || result.Error == InteractionCommandError.UnknownCommand)
            return Task.CompletedTask;

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
            return Task.CompletedTask;

        var reason = GetErrorReason(result);
        return SendErrorMessageAsync(reason, context.Interaction, result.ErrorReason);
    }

    private Task HandleInteractionAsync(SocketInteraction interaction)
    {
        var ctx = new SocketInteractionContext(Client, interaction);
        return _interactionService.ExecuteCommandAsync(ctx, _provider);
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
            embed.WithDescription($"```{description}```");

        return interaction.HasResponded
            ? interaction.FollowupAsync(embed: embed.Build(), ephemeral: true)
            : interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
    }
}
