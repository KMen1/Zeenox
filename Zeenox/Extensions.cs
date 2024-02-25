using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Discord;
using Zeenox.Models.Socket;
using Zeenox.Players;

namespace Zeenox;

public static class Extensions
{
    public static string ToTimeString(this TimeSpan timeSpan)
    {
        return timeSpan.TotalHours < 1
            ? timeSpan.ToString(@"mm\:ss")
            : timeSpan.ToString(timeSpan.TotalDays < 1 ? @"hh\:mm\:ss" : @"dd\:hh\:mm\:ss");
    }

    public static bool TryGetUserId(
        this ClaimsIdentity? claimsPrincipal,
        [NotNullWhen(true)] out ulong? userId
    )
    {
        var result = ulong.TryParse(claimsPrincipal?.FindFirst("USER_ID")?.Value, out var id);
        userId = id;
        return result;
    }

    public static bool TryGetUserId(
        this ClaimsPrincipal? claimsPrincipal,
        [NotNullWhen(true)] out ulong? userId
    )
    {
        var result = ulong.TryParse(claimsPrincipal?.FindFirst("USER_ID")?.Value, out var id);
        userId = id;
        return result;
    }

    public static bool TryGetGuildId(
        this ClaimsIdentity? claimsPrincipal,
        [NotNullWhen(true)] out ulong? guildId
    )
    {
        var result = ulong.TryParse(claimsPrincipal?.FindFirst("GUILD_ID")?.Value, out var id);
        guildId = id;
        return result;
    }

    public static bool TryGetGuildId(
        this ClaimsPrincipal? claimsPrincipal,
        [NotNullWhen(true)] out ulong? guildId
    )
    {
        var result = ulong.TryParse(claimsPrincipal?.FindFirst("GUILD_ID")?.Value, out var id);
        guildId = id;
        return result;
    }

    public static Task SendTextAsync(this WebSocket socket, string text)
    {
        return socket.SendAsync(
            Encoding.UTF8.GetBytes(text).ToArray(),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None
        );
    }

    public static async Task SendSocketMessagesAsync(
        this WebSocket socket,
        SocketPlayer player,
        bool updatePlayer,
        bool updateTrack,
        bool updateQueue,
        bool updateActions
    )
    {
        if (updatePlayer)
        {
            await socket
                .SendTextAsync(JsonSerializer.Serialize(new UpdatePlayerPayload(player)))
                .ConfigureAwait(false);
        }

        if (updateTrack)
        {
            await socket
                .SendTextAsync(JsonSerializer.Serialize(new UpdateTrackPayload(player.CurrentItem)))
                .ConfigureAwait(false);
        }

        if (updateQueue)
        {
            await socket
                .SendTextAsync(JsonSerializer.Serialize(new UpdateQueuePayload(player.Queue)))
                .ConfigureAwait(false);
        }

        if (updateActions)
        {
            await socket
                .SendTextAsync(JsonSerializer.Serialize(new AddActionPayload(player)))
                .ConfigureAwait(false);
        }
    }

    public static bool IsUserListening(this SocketPlayer player, IUser user)
    {
        return player.VoiceChannel.ConnectedUsers.Any(x => x.Id == user.Id);
    }
    
    public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(items);

        if (list is List<T> asList)
        {
            asList.AddRange(items);
        }
        else
        {
            foreach (var item in items)
            {
                list.Add(item);
            }
        }
    }
}
