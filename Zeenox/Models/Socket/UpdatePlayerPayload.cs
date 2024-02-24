using Zeenox.Dtos;
using Zeenox.Enums;
using Zeenox.Players;

namespace Zeenox.Models.Socket;

public class UpdatePlayerPayload(SocketPlayerDTO playerDto) : IPayload
{
    public PayloadType Type { get; } = PayloadType.UpdatePlayer;
    public SocketPlayerDTO Player { get; } = playerDto;

    public UpdatePlayerPayload(SocketPlayer player) : this(new SocketPlayerDTO(player))
    {
    }
}