using Discord;
using Zeenox.Models.Player;
using Zeenox.Models.Socket;
using ActionType = Zeenox.Enums.ActionType;

namespace Zeenox.Models.Actions.Player;

public class PlayAction(IUser user, ExtendedTrackItem trackItem) : Action(user, ActionType.Play)
{
    public TrackDto Track { get; } = new(trackItem);

    public override string Stringify()
    {
        return $"played: {trackItem.Title}";
    }
}