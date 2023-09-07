using Discord;

namespace Zeenox.Models;

public class DiscordUserData
{
    private DiscordUserData(string username, string displayName, string avatarUrl)
    {
        Username = username;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
    }

    public string Username { get; init; }
    public string DisplayName { get; init; }
    public string AvatarUrl { get; init; }

    public static DiscordUserData FromUser(IUser user)
    {
        return new DiscordUserData(user.Username, user.GlobalName, user.GetAvatarUrl());
    }

    public static DiscordUserData Empty => new(string.Empty, string.Empty, string.Empty);
}
