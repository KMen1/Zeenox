using System.Text.Json.Serialization;
using Discord;

namespace Zeenox.Dtos;

[method: JsonConstructor]
public class SocketUserDTO(string username, string displayName, string avatarUrl)
{
    public string Username { get; } = username;
    public string DisplayName { get; } = displayName;
    public string AvatarUrl { get; } = avatarUrl;

    public SocketUserDTO(IUser user)
        : this(user.Username, user.GlobalName, user.GetAvatarUrl()) { }
}
