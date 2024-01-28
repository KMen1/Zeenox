using Discord.WebSocket;

namespace Zeenox.Models.Socket;

public class BasicDiscordGuild(string id, string name, string iconUrl, PlayerResumeSessionDto? resumeSession)
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public string IconUrl { get; } = iconUrl;
    public PlayerResumeSessionDto? ResumeSession { get; } = resumeSession;

    public BasicDiscordGuild(SocketGuild guild, PlayerResumeSessionDto? resumeSession) : this(guild.Id.ToString(), guild.Name, guild.IconUrl, resumeSession)
    {
    }
}