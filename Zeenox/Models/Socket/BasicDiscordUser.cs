using System.Text.Json.Serialization;
using Discord;

namespace Zeenox.Models.Socket;

public class BasicDiscordUser
{
    public string Username { get; init; }
    public string DisplayName { get; init; }
    public string? AvatarUrl { get; init; }

    public BasicDiscordUser(IUser? user) : this(user?.Username ?? "", user?.GlobalName ?? "", user?.GetAvatarUrl())
    {
    }

    [JsonConstructor]
    public BasicDiscordUser(string username, string displayName, string? avatarUrl)
    {
        Username = username;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
    }
}