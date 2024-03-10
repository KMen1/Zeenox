using Zeenox.Enums;

namespace Zeenox.Models.Socket;

public interface IPayload
{
    public PayloadType Type { get; }
}