using Discord.WebSocket;

namespace Zeenox.Models.Socket;

public class GuildDto(string id, string name, string iconUrl)
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public string IconUrl { get; } = iconUrl;

    public GuildDto(SocketGuild guild) : this(guild.Id.ToString(), guild.Name, guild.IconUrl)
    {
    }
}