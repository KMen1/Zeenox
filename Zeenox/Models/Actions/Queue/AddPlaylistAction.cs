using Discord;
using Lavalink4NET.Rest.Entities.Tracks;
using Zeenox.Dtos;
using Zeenox.Models.Player;
using ActionType = Zeenox.Enums.ActionType;

namespace Zeenox.Models.Actions.Queue;

public class AddPlaylistAction(
    IUser user,
    PlaylistInformation? playlistInformation,
    IEnumerable<ExtendedTrackItem> trackItems) : Action(user, ActionType.AddPlaylist)
{
    public PlaylistDTO? Playlist { get; } =
        playlistInformation is not null ? new PlaylistDTO(playlistInformation) : null;

    public List<TrackDTO> Tracks { get; } = trackItems.Select(x => new TrackDTO(x)).ToList();
    public int Count => Tracks.Count;
}