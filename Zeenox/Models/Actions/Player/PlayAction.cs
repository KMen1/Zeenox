using Discord;
using Zeenox.Dtos;
using Zeenox.Models.Player;
using ActionType = Zeenox.Enums.ActionType;

namespace Zeenox.Models.Actions.Player;

public class PlayAction(IUser user, ExtendedTrackItem trackItem) : Action(user, ActionType.Play)
{
    public TrackDTO Track { get; } = new(trackItem);

    public override string Stringify()
    {
        return $"played: {Track.Title}";
    }
}