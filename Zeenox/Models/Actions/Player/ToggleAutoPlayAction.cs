using Discord;
using ActionType = Zeenox.Enums.ActionType;

namespace Zeenox.Models.Actions.Player;

public class ToggleAutoPlayAction(IUser user, bool isAutoPlayEnabled) : Action(user, ActionType.ToggleAutoPlay)
{
    public bool IsAutoPlayEnabled { get; } = isAutoPlayEnabled;
}