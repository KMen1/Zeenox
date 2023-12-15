using Discord;
using Zeenox.Enums;
using ActionType = Zeenox.Enums.ActionType;

namespace Zeenox.Models.Actions.Queue;

public class QueueAction(IUser user, QueueActionType type) : Action(user, ActionType.Queue)
{
    public QueueActionType QueueActionType { get; } = type;

    public override string Stringify()
    {
        return QueueActionType switch
        {
            QueueActionType.AddTrack => ((EnqueueTrackAction)this).Stringify(),
            QueueActionType.AddPlaylist => ((EnqueuePlaylistAction)this).Stringify(),
            QueueActionType.Clear => "cleared the queue",
            QueueActionType.Distinct => "removed duplicate tracks from the queue",
            QueueActionType.Reverse => "reversed the queue",
            QueueActionType.Shuffle => "shuffled the queue",
            _ => $"Unknown queue action {QueueActionType}"
        };
    }
}