using Zeenox.Enums;
using Zeenox.Models.Socket;
using Zeenox.Players;

namespace Zeenox.Dtos;

public class FullPlayerDTO(SocketPlayer player, ResumeSessionDTO? resumeSessionDto) : IPayload
{
    public PayloadType Type { get; } = PayloadType.Initialize | PayloadType.UpdatePlayer | PayloadType.UpdateQueue | PayloadType.UpdateTrack | PayloadType.UpdateActions;
    public string VoiceChannelName { get; } = player.VoiceChannel.Name;
    public long StartedAt { get; } = player.StartedAt.ToUnixTimeSeconds();
    public SocketPlayerDTO State { get; } = new(player);
    public TrackDTO? CurrentTrack { get; } = player.CurrentItem is not null ? new TrackDTO(player.CurrentItem) : null;
    public QueueDTO Queue { get; } = new(player.Queue);
    public ResumeSessionDTO? ResumeSession { get; } = resumeSessionDto;
    public object Actions { get; } = player.GetActionsForSerialization();
}