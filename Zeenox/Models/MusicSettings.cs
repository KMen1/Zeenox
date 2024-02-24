namespace Zeenox.Models;

public class MusicSettings
{
    public List<ulong> WhiteListRoles { get; set; } = [];
    public List<ulong> WhitelistChannels { get; set; } = [];
    public bool IsExclusiveControl { get; set; } = false;
    public int DefaultVolume { get; set; } = 100;
    public bool BlockPlaylists { get; set; } = false;
    public int MaxTrackLength { get; set; } = 0;
    public int MaxPlaylistLength { get; set; } = 0;
}
