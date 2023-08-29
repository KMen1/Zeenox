using MongoDB.Bson.Serialization.Attributes;
using Zeenox.Enums;

namespace Zeenox.Models;

public class GuildConfig
{
    public GuildConfig(ulong guildId)
    {
        GuildId = guildId;
    }

    [BsonId]
    public ulong GuildId { get; set; }
    public Langcode Language { get; set; } = Langcode.ENG;
    public MusicSettings MusicSettings { get; set; } = new();
}
