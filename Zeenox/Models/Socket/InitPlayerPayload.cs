using Zeenox.Dtos;
using Zeenox.Enums;
using Zeenox.Players;

namespace Zeenox.Models.Socket;

public class InitPlayerPayload(SocketPlayer player, ResumeSessionDTO? resumeSession) : IPayload
{
    public PayloadType Type { get; } = PayloadType.InitPlayer;
    public string VoiceChannelName { get; } = player.VoiceChannel.Name;
    public long StartedAt { get; } = player.StartedAt.ToUnixTimeSeconds();
    public int Position { get; } = player.Position.HasValue ? player.Position.Value.Position.Seconds : 0;
    public SocketPlayerDTO Player { get; } = new(player);
    public TrackDTO? CurrentTrack { get; } = player.CurrentItem is not null ? new TrackDTO(player.CurrentItem) : null;
    public QueueDTO Queue { get; } = new(player.Queue);
    public ResumeSessionDTO? ResumeSession { get; } = resumeSession;
    public object Actions { get; } = player.GetActionsForSerialization();
}