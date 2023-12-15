using Discord;
using Zeenox.Models.Player;
using Zeenox.Models.Socket;
using ActionType = Zeenox.Enums.ActionType;

namespace Zeenox.Models.Actions.Player;

public class RewindAction(IUser user, ExtendedTrackItem trackItem) : Action(user, ActionType.Rewind)
{
    public TrackDto Track { get; } = new(trackItem);
    
    public override string Stringify()
    {
        return $"rewound to: {trackItem.Title}";
    }
}