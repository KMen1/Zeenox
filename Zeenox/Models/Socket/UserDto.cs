using System.Text.Json.Serialization;
using Discord;

namespace Zeenox.Models.Socket;

public class UserDto
{
    public string Username { get; init; }
    public string DisplayName { get; init; }
    public string? AvatarUrl { get; init; }

    public UserDto(IUser? user) : this(user?.Username ?? "", user?.GlobalName ?? "", user?.GetAvatarUrl())
    {
    }

    [JsonConstructor]
    public UserDto(string username, string displayName, string? avatarUrl)
    {
        Username = username;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
    }
}