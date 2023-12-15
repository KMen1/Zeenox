using Zeenox.Players;

namespace Zeenox.Models.Socket;

public sealed class ActionDto(object? action = null) : ISocketMessageData
{
    public string Type { get; } = "player-action";
    
    public object? Action { get; } = action;
    
    public ActionDto(LoggedPlayer player) : this(player.GetActionForSerialization())
    {
    }
}