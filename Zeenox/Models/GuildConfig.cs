using MongoDB.Bson.Serialization.Attributes;
using Zeenox.Enums;

namespace Zeenox.Models;

public class GuildConfig(ulong guildId)
{
    [BsonId] public ulong GuildId { get; set; } = guildId;

    public Language Language { get; set; } = Language.English;
    public MusicSettings MusicSettings { get; set; } = new();
}