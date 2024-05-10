using Discord;
using Zeenox.Dtos;
using Zeenox.Models.Player;
using ActionType = Zeenox.Enums.ActionType;

namespace Zeenox.Models.Actions.Queue;

public class AddTrackAction(IUser user, ExtendedTrackItem trackItem) :Action(user, ActionType.AddTrack)
{
    public TrackDTO Track { get; } = new(trackItem);
}