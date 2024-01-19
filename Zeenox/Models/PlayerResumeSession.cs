using MongoDB.Bson.Serialization.Attributes;
using Zeenox.Models.Player;
using Zeenox.Players;

namespace Zeenox.Models;

public class PlayerResumeSession(ulong guildId, ulong channelId, ExtendedTrackDto currentTrackId, List<ExtendedTrackDto> queue)
{
    [BsonId] public ulong GuildId { get; set; } = guildId;
    public ulong ChannelId { get; set; } = channelId;
    public ExtendedTrackDto CurrentTrack { get; set; } = currentTrackId;
    public List<ExtendedTrackDto> Queue { get; set; } = queue; 
    public long Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeSeconds();

    public PlayerResumeSession(LoggedPlayer player) : this(player.GuildId, player.VoiceChannelId, new ExtendedTrackDto(player.CurrentItem ?? player.LastCurrentItem ?? (ExtendedTrackItem)player.Queue[0]), player.Queue.Select(x => new ExtendedTrackDto((ExtendedTrackItem)x)).ToList()) { }
}

public class ExtendedTrackDto(string id, ulong requesterId)
{
    public string Id { get; set; } = id;
    public ulong RequesterId { get; set; } = requesterId;
    
    public ExtendedTrackDto(ExtendedTrackItem track) : this(track.Track.Track.ToString(), track.RequestedBy?.Id ?? 0) { }
}