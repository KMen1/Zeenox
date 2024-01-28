using Zeenox.Enums;

namespace Zeenox.Models.Socket;

public class InitPlayerPayload(string voiceChannelName, long startedAt, int position, PlayerResumeSessionDto? resumeSession) : IPayload
{
    public PayloadType Type { get; } = PayloadType.InitPlayer;
    public string VoiceChannelName { get; } = voiceChannelName;
    public long StartedAt { get; } = startedAt;   
    public int Position { get; } = position;
    public PlayerResumeSessionDto? ResumeSession { get; } = resumeSession;
}