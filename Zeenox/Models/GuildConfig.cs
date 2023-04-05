using MongoDB.Bson.Serialization.Attributes;
using Zeenox.Enums;

namespace Zeenox.Models;

public class GuildConfig
{
    public GuildConfig(ulong guildId)
    {
        GuildId = guildId;
        Language = Language.English;
        MusicSettings = new MusicSettings
        {
            IsExclusiveControl = false,
            IsDjOnly = false,
            DjRoleIds = new List<ulong>(),
            RequestChannelId = null,
            DefaultVolume = 100,
            IsPlaylistAllowed = true,
            TrackLengthLimit = 10,
            PlaylistLengthLimit = 100
        };
    }

    [BsonId]
    public ulong GuildId { get; set; }
    public Language Language { get; set; }
    public MusicSettings MusicSettings { get; set; }
}

public class MusicSettings
{
    public bool IsExclusiveControl { get; set; }
    public bool IsDjOnly { get; set; }
    public List<ulong> DjRoleIds { get; set; }
    public ulong? RequestChannelId { get; set; }
    public int DefaultVolume { get; set; }
    public bool IsPlaylistAllowed { get; set; }
    public int TrackLengthLimit { get; set; }
    public int PlaylistLengthLimit { get; set; }
}
