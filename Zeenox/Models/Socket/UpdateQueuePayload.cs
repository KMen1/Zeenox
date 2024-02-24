using Lavalink4NET.Players.Queued;
using Zeenox.Dtos;
using Zeenox.Enums;

namespace Zeenox.Models.Socket;

public class UpdateQueuePayload(QueueDTO queueDto) : IPayload
{
    public PayloadType Type { get; } = PayloadType.UpdateQueue;
    public QueueDTO Queue { get; } = queueDto;

    public UpdateQueuePayload(ITrackQueue queue) : this(new QueueDTO(queue))
    {
    }
}