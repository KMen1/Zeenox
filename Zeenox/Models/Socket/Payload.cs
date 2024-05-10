using System.Text.Json.Serialization;
using Zeenox.Dtos;
using Zeenox.Enums;

namespace Zeenox.Models.Socket;

public class Payload : IPayload
{
    public PayloadType Type { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SocketPlayerDTO? State { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TrackDTO? CurrentTrack { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public QueueDTO? Queue { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Actions { get; set; }
}
