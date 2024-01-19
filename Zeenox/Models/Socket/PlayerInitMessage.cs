namespace Zeenox.Models.Socket;

public class PlayerInitMessage(string voiceChannelName, long startedAt, int position, PlayerResumeSessionDto? resumeSession) : ISocketMessageData
{
    public string Type { get; } = "player-init";
    public string VoiceChannelName { get; } = voiceChannelName;
    public long StartedAt { get; } = startedAt;   
    public int Position { get; } = position;
    public PlayerResumeSessionDto? ResumeSession { get; } = resumeSession;
}