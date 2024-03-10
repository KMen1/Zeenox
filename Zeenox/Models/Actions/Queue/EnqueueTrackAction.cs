using Discord;
using Zeenox.Dtos;
using Zeenox.Enums;
using Zeenox.Models.Player;

namespace Zeenox.Models.Actions.Queue;

public class EnqueueTrackAction(IUser user, ExtendedTrackItem trackItem) : QueueAction(user, QueueActionType.AddTrack)
{
    public TrackDTO Track { get; } = new(trackItem);

    public override string Stringify() => $"enqueued: {trackItem.Title}";
}