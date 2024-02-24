using Zeenox.Enums;
using Zeenox.Players;

namespace Zeenox.Models.Socket;

public class AddActionsPayload(object actions) : IPayload
{
    public PayloadType Type { get; } = PayloadType.AddActions;
    public object Actions { get; } = actions;
    
    public AddActionsPayload(LoggedPlayer player) : this(player.GetActionsForSerialization())
    {
    }
}