using MongoDB.Bson.Serialization.Attributes;

namespace Zeenox.Models;

public class User
{
    public User(ulong userId)
    {
        UserId = userId;
    }

    [BsonId]
    public ulong UserId { get; set; }
    public List<string> FavoriteSongs { get; set; } = new();
}
