using Discord.WebSocket;

namespace Zeenox.Models;

public class GuildInfo
{
    private GuildInfo(string id, string name, string iconUrl)
    {
        Id = id;
        Name = name;
        IconUrl = iconUrl;
    }

    public string Id { get; init; }
    public string Name { get; init; }
    public string IconUrl { get; init; }

    public static GuildInfo FromSocketGuild(SocketGuild guild)
    {
        return new GuildInfo(guild.Id.ToString(), guild.Name, guild.IconUrl);
    }
}
