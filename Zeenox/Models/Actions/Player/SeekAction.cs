using Discord;
using ActionType = Zeenox.Enums.ActionType;

namespace Zeenox.Models.Actions.Player;

public class SeekAction(IUser user, int position) : Action(user, ActionType.Seek)
{
    public int Position { get; set; } = position;

    public override string Stringify() => $"seeked to {TimeSpan.FromSeconds(Position).ToString()}";
}