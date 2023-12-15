using MongoDB.Bson.Serialization.Attributes;
using Zeenox.Players;
using Action = Zeenox.Models.Actions.Action;

namespace Zeenox.Models;

public class PlayerLog(ulong guildId, ulong channelId, List<Action> actions)
{
    [BsonId] public string Id { get; set; }
    public ulong GuildId { get; set; } = guildId;
    public ulong ChannelId { get; set; } = channelId;
    public List<Action> Actions { get; set; } = actions;

    public PlayerLog(LoggedPlayer player) : this(player.GuildId, player.VoiceChannelId, player.Actions) { }
}