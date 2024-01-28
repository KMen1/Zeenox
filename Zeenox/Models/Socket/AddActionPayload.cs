using Zeenox.Enums;
using Zeenox.Players;

namespace Zeenox.Models.Socket;

public sealed class AddActionPayload(object? action = null) : IPayload
{
    public PayloadType Type { get; } = PayloadType.AddAction;
    
    public object? Action { get; } = action;
    
    public AddActionPayload(LoggedPlayer player) : this(player.GetActionForSerialization())
    {
    }
}