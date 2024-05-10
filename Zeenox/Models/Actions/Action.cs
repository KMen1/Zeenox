using System.Text.Json;
using System.Text.Json.Serialization;
using Discord;
using Zeenox.Dtos;
using ActionType = Zeenox.Enums.ActionType;

namespace Zeenox.Models.Actions;

public class UserJsonConverter : JsonConverter<IUser>
{
    public override IUser? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => null;

    public override void Write(
        Utf8JsonWriter writer,
        IUser user,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, new SocketUserDTO(user), options);
    }
}

public class Action(IUser user, ActionType type) : IAction
{
    [JsonConverter(typeof(UserJsonConverter))]
    public IUser User { get; } = user;

    public ActionType Type { get; } = type;
    public long Timestamp { get; } = DateTimeOffset.Now.ToUnixTimeSeconds();
    public string Message => this.GetMessage();
}