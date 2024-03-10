using Discord.WebSocket;

namespace Zeenox.Dtos;

public class SocketGuildDTO(
    string id,
    string name,
    string iconUrl,
    TrackDTO? currentTrack,
    string? voiceChannel,
    ResumeSessionDTO? resumeSession)
{
    public SocketGuildDTO(SocketGuild guild,
                          TrackDTO? currentTrack,
                          string? voiceChannel,
                          ResumeSessionDTO? resumeSession) : this(guild.Id.ToString(), guild.Name, guild.IconUrl,
                                                                  currentTrack, voiceChannel, resumeSession) { }

    public string Id { get; } = id;
    public string Name { get; } = name;
    public string IconUrl { get; } = iconUrl;
    public TrackDTO? CurrentTrack { get; set; } = currentTrack;
    public ResumeSessionDTO? ResumeSession { get; } = resumeSession;
    public string? ConnectedVoiceChannel { get; set; } = voiceChannel;
}