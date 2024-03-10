using MongoDB.Bson.Serialization.Attributes;
using Zeenox.Models.Player;
using Zeenox.Players;

namespace Zeenox.Models;

public class ResumeSession(ulong guildId, ulong channelId, TrackStore currentTrackId, List<TrackStore> queue)
{
    public ResumeSession(LoggedPlayer player) : this(player.GuildId, player.VoiceChannelId,
                                                     new TrackStore(
                                                         player.CurrentItem ?? player.LastCurrentItem ??
                                                         (ExtendedTrackItem)player.Queue[0]),
                                                     player.Queue.Select(x => new TrackStore((ExtendedTrackItem)x))
                                                           .ToList()) { }

    [BsonId] public ulong GuildId { get; set; } = guildId;
    public ulong ChannelId { get; set; } = channelId;
    public TrackStore CurrentTrack { get; set; } = currentTrackId;
    public List<TrackStore> Queue { get; set; } = queue;
    public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeSeconds();
}