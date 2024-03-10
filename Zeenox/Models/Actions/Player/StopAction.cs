using Discord;
using ActionType = Zeenox.Enums.ActionType;

namespace Zeenox.Models.Actions.Player;

public class StopAction(IUser user) : Action(user, ActionType.Stop)
{
    public override string Stringify() => "stopped the player";
}