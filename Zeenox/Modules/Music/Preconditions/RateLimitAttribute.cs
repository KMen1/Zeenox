/*!
 * Discord Rate limit v1.5 (https://jalaljaleh.github.io/)
 * Copyright 2021-2022 Jalal Jaleh
 * Licensed under MIT (https://github.com/jalaljaleh/Template.Discord.Bot/blob/master/LICENSE.txt)
 * Original (https://github.com/jalaljaleh/Template.Discord.Bot/)
 */

using System.Collections.Concurrent;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
// ReSharper disable PossibleMultipleEnumeration

namespace Zeenox.Modules.Music.Preconditions
{
    public class RateLimit : PreconditionAttribute
    {
        public static ConcurrentDictionary<ulong, List<RateLimitItem>> Items = new ();
        private static DateTime _removeExpiredCommandsTime = DateTime.MinValue;
        private readonly RateLimitType? _context;
        private readonly RateLimitBaseType _baseType;
        private readonly int _requests;
        private readonly int _seconds;
        public RateLimit(int seconds = 4, int requests = 1, RateLimitType context = RateLimitType.User, RateLimitBaseType baseType = RateLimitBaseType.BaseOnCommandInfo)
        {
            _context = context;
            _requests = requests;
            _seconds = seconds;
            _baseType = baseType;
        }
        public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            // clear old expired commands every 30m
            if (DateTime.UtcNow > _removeExpiredCommandsTime)
            {
                _ = Task.Run(async () =>
                {
                    await ClearExpiredCommands().ConfigureAwait(false);
                    _removeExpiredCommandsTime = DateTime.UtcNow.AddMinutes(30);
                });
            }

            ulong id = _context.Value switch
            {
                RateLimitType.User => context.User.Id,
                RateLimitType.Channel => context.Channel.Id,
                RateLimitType.Guild => context.Guild.Id,
                RateLimitType.Global => 0,
                _ => 0
            };

            var contextId = _baseType switch
            {
                RateLimitBaseType.BaseOnCommandInfo => commandInfo.Module.Name + "//" + commandInfo.Name + "//" + commandInfo.MethodName,
                RateLimitBaseType.BaseOnCommandInfoHashCode => commandInfo.GetHashCode().ToString(),
                RateLimitBaseType.BaseOnSlashCommandName => (context.Interaction as SocketSlashCommand).CommandName,
                RateLimitBaseType.BaseOnMessageComponentCustomId => (context.Interaction as SocketMessageComponent).Data.CustomId,
                RateLimitBaseType.BaseOnAutocompleteCommandName => (context.Interaction as SocketAutocompleteInteraction).Data.CommandName,
                RateLimitBaseType.BaseOnApplicationCommandName => (context.Interaction as SocketApplicationCommand).Name,
                _ => "unknown"
            };

            var dateTime = DateTime.UtcNow;

            var target = Items.GetOrAdd(id, new List<RateLimitItem>());

            var commands = target.Where(
                a =>
                a.Command == contextId
            );

            foreach (var c in commands.ToList())
                if (dateTime >= c.ExpireAt)
                    target.Remove(c);

            if (commands.Count() < _requests)
            {
                target.Add(new RateLimitItem()
                {
                    Command = contextId,
                    ExpireAt = DateTime.UtcNow + TimeSpan.FromSeconds(_seconds)
                });
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            return Task.FromResult(PreconditionResult.FromError($"This command is usable <t:{((DateTimeOffset)target.Last().ExpireAt).ToUnixTimeSeconds()}:R>."));
        }
        public static Task ClearExpiredCommands()
        {
            foreach (var doc in Items)
            {
                var utcTime = DateTime.UtcNow;
                foreach (var command in doc.Value.Where(a => utcTime > a.ExpireAt).ToList())
                    doc.Value.Remove(command);
            }
            return Task.CompletedTask;
        }
        
        public class RateLimitItem
        {
            public string Command { get; set; } = null!;
            public DateTime ExpireAt { get; set; }
        }
        public enum RateLimitType
        {
            User,
            Channel,
            Guild,
            Global
        }
        public enum RateLimitBaseType
        {
            BaseOnCommandInfo,
            BaseOnCommandInfoHashCode,
            BaseOnSlashCommandName,
            BaseOnMessageComponentCustomId,
            BaseOnAutocompleteCommandName,
            BaseOnApplicationCommandName
        }
    }
}