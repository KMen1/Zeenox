using Discord;
using ActionType = Zeenox.Enums.ActionType;

namespace Zeenox.Models.Actions.Player;

public class ResumeAction(IUser user) : Action(user, ActionType.Resume)
{
    public override string Stringify()
    {
        return "resumed the player";
    }
}