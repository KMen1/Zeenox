using Lavalink4NET.Rest.Entities.Tracks;

namespace Zeenox.Dtos;

public class SearchResultDTO(List<TrackDTO> tracks, PlaylistDTO? playlist)
{
    public SearchResultDTO(TrackLoadResult result) : this(result.Tracks.Select(x => new TrackDTO(x)).ToList(),
                                                          result.IsPlaylist ? new PlaylistDTO(result) : null) { }

    public List<TrackDTO> Tracks { get; } = tracks;
    public PlaylistDTO? Playlist { get; } = playlist;
}