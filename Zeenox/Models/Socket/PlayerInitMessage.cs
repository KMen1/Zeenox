namespace Zeenox.Models.Socket;

public class PlayerInitMessage(string voiceChannelName, long startedAt, int position) : ISocketMessageData
{
    public string Type { get; } = "player-init";
    public string VoiceChannelName { get; } = voiceChannelName;
    public long StartedAt { get; } = startedAt;   
    public int Position { get; } = position;
}