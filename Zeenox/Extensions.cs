using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Discord;
using Zeenox.Models;
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

    public static ulong? GetGuildId(this ClaimsIdentity claimsPrincipal)
    {
        return ulong.TryParse(claimsPrincipal.FindFirst("GUILD_ID")?.Value, out var guildId)
            ? guildId
            : null;
    }

    public static ulong? GetUserId(this ClaimsIdentity claimsPrincipal)
    {
        return ulong.TryParse(claimsPrincipal.FindFirst("USER_ID")?.Value, out var userId)
            ? userId
            : null;
    }

    public static ulong? GetGuildId(this ClaimsPrincipal claimsPrincipal)
    {
        return ulong.TryParse(claimsPrincipal.FindFirst("GUILD_ID")?.Value, out var guildId)
            ? guildId
            : null;
    }

    public static ulong? GetUserId(this ClaimsPrincipal claimsPrincipal)
    {
        return ulong.TryParse(claimsPrincipal.FindFirst("USER_ID")?.Value, out var userId)
            ? userId
            : null;
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

    public static async Task InitSocketAsync(
        this WebSocket socket,
        LoggedPlayer player,
        PlayerResumeSessionDto? resumeSessionDto
    )
    {
        await socket
            .SendTextAsync(
                JsonSerializer.Serialize(
                    new InitPlayerPayload(
                        player.VoiceChannel.Name,
                        player.StartedAt.ToUnixTimeSeconds(),
                        player.Position?.Position.Seconds ?? 0,
                        resumeSessionDto
                    )
                )
            )
            .ConfigureAwait(false);
        await socket
            .SendTextAsync(JsonSerializer.Serialize(new UpdatePlayerPayload(player)))
            .ConfigureAwait(false);
        await socket
            .SendTextAsync(JsonSerializer.Serialize(new TrackPayload(player.CurrentItem)))
            .ConfigureAwait(false);
        await socket
            .SendTextAsync(JsonSerializer.Serialize(new UpdateQueuePayload(player)))
            .ConfigureAwait(false);
        await socket
            .SendTextAsync(JsonSerializer.Serialize(new AddActionsPayload(player)))
            .ConfigureAwait(false);
    }

    public static async Task SendSocketMessagesAsync(
        this WebSocket socket,
        LoggedPlayer player,
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
                .SendTextAsync(JsonSerializer.Serialize(new TrackPayload(player.CurrentItem)))
                .ConfigureAwait(false);
        }

        if (updateQueue)
        {
            await socket
                .SendTextAsync(JsonSerializer.Serialize(new UpdateQueuePayload(player)))
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
}
