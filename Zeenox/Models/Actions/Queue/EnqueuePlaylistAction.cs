using Discord;
using Lavalink4NET.Rest.Entities.Tracks;
using Zeenox.Dtos;
using Zeenox.Enums;
using Zeenox.Models.Player;

namespace Zeenox.Models.Actions.Queue;

public class EnqueuePlaylistAction(IUser user, PlaylistInformation? playlistInformation, IEnumerable<ExtendedTrackItem> trackItems) : QueueAction(user, QueueActionType.AddPlaylist)
{
    public PlaylistDTO? Playlist { get; } = playlistInformation is not null ? new(playlistInformation) : null;
    public List<TrackDTO> Tracks { get; } = trackItems.Select(x => new TrackDTO(x)).ToList();
    public int Count => Tracks.Count;
    
    public override string Stringify()
    {
        return $"enqueued {Count} tracks";
    }
}