using MongoDB.Bson.Serialization.Attributes;
using Zeenox.Enums;

namespace Zeenox.Models;

public class GuildConfig(ulong guildId)
{
    [BsonId] public ulong GuildId { get; set; } = guildId;

    public Langcode Language { get; set; } = Langcode.ENG;
    public MusicSettings MusicSettings { get; set; } = new();
}