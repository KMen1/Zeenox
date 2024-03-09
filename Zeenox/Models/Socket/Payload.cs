using Zeenox.Enums;

namespace Zeenox.Models.Socket;

public readonly struct Payload(PayloadType type) : IPayload
{
    public PayloadType Type { get; } = type;
}