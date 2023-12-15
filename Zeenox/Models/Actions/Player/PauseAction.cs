using Discord;
using ActionType = Zeenox.Enums.ActionType;

namespace Zeenox.Models.Actions.Player;

public class PauseAction(IUser user) : Action(user, ActionType.Pause)
{
    public override string Stringify()
    {
        return "paused the player";
    }
}