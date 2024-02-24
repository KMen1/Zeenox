using Zeenox.Enums;
using Zeenox.Players;

namespace Zeenox.Models.Socket;

public sealed class AddActionPayload(object action) : IPayload
{
    public PayloadType Type { get; } = PayloadType.AddAction;
    public object Action { get; } = action;
    
    public AddActionPayload(SocketPlayer player) : this(player.GetActionForSerialization())
    {
    }
}