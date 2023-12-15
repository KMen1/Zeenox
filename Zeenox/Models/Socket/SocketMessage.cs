namespace Zeenox.Models.Socket;

public class SocketMessage(ISocketMessageData data)
{
    public ISocketMessageData Data { get; init; } = data;
}