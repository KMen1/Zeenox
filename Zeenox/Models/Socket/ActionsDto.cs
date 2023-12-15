using Zeenox.Players;

namespace Zeenox.Models.Socket;

public class ActionsDto(object? actions = null) : ISocketMessageData
{
    public string Type { get; } = "player-actions";
    public object? Actions { get; } = actions;
    
    public ActionsDto(LoggedPlayer player) : this(player.GetActionsForSerialization())
    {
    }
}