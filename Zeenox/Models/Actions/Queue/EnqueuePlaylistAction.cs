using Discord;
using Zeenox.Enums;
using Zeenox.Models.Player;
using Zeenox.Models.Socket;

namespace Zeenox.Models.Actions.Queue;

public class EnqueuePlaylistAction(IUser user, IEnumerable<ExtendedTrackItem> trackItems) : QueueAction(user, QueueActionType.AddPlaylist)
{
    public List<TrackDto> Tracks { get; } = trackItems.Select(x => new TrackDto(x)).ToList();
    public int Count => Tracks.Count;
    
    public override string Stringify()
    {
        return $"enqueued {Count} tracks";
    }
}