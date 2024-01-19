using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Discord;
using Zeenox.Models.Socket;
using ActionType = Zeenox.Enums.ActionType;

namespace Zeenox.Models.Actions;

public class UserJsonConverter : JsonConverter<IUser>
{
    public override IUser? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return null;
    }

    public override void Write(
        Utf8JsonWriter writer,
        IUser user,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, new UserDto(user), options);
    }
}

public class DateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTimeOffset.MaxValue;
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateTimeOffset dateTimeOffset,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, dateTimeOffset.ToUnixTimeSeconds(), options);
    }
}

public abstract class Action(IUser user, ActionType type) : IAction
{
    [JsonConverter(typeof (UserJsonConverter))]
    public IUser User { get; } = user;
    public ActionType Type { get; } = type;
    [JsonConverter(typeof (DateTimeOffsetJsonConverter))]
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.Now;

    public virtual string Stringify()
    {
        return "";
    }

    public string StringifyFull()
    {
        var sb = new StringBuilder();
        sb.Append($"[<t:{Timestamp.ToUnixTimeSeconds()}:t>] {User.Mention} ");
        sb.Append(((IAction)this).Stringify());
        return sb.ToString();
    }
}