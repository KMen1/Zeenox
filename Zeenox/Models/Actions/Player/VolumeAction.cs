using Discord;
using ActionType = Zeenox.Enums.ActionType;

namespace Zeenox.Models.Actions.Player;

public class VolumeAction(IUser user, int volume, ActionType type) : Action(user, type)
{
    public int Volume { get; } = volume;
}