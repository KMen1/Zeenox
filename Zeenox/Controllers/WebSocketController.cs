using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Zeenox.Models;
using Zeenox.Services;

namespace Zeenox.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]/[action]")]
[ApiVersion("1.0")]
public class WebSocketController : ControllerBase
{
    private readonly MusicService _musicService;

    public WebSocketController(MusicService musicService)
    {
        _musicService = musicService;
    }

    [HttpGet(Name = "CreateWebSocket")]
    public async Task CreateWebSocket(ulong guildId, ulong userId)
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets
                .AcceptWebSocketAsync()
                .ConfigureAwait(false);
            await SetupSocketAsync(webSocket, guildId, userId).ConfigureAwait(false);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }

    private async Task SetupSocketAsync(WebSocket socket, ulong guildId, ulong userId)
    {
        var buffer = new byte[1024 * 4];
        var receiveResult = await socket
            .ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None)
            .ConfigureAwait(false);

        var rawData = Encoding.UTF8.GetString(
            new ArraySegment<byte>(buffer, 0, receiveResult.Count)
        );
        //var message = JsonSerializer.Deserialize<InitSocketMessage>(rawData)!;

        _musicService.AddWebSocket(guildId, socket);

        while (!receiveResult.CloseStatus.HasValue)
        {
            var player = await _musicService.TryGetPlayerAsync(guildId).ConfigureAwait(false);
            if (player is null)
                break;
            var messageToSend = SocketMessage.FromZeenoxPlayer(player, updatePlayer: true);

            await socket
                .SendAsync(
                    Encoding.UTF8.GetBytes(JsonSerializer.Serialize(messageToSend)),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                )
                .ConfigureAwait(false);
            await Task.Delay(1000).ConfigureAwait(false);
        }

        _musicService.RemoveWebSocket(guildId, socket);

        await socket
            .CloseAsync(
                receiveResult.CloseStatus.Value,
                receiveResult.CloseStatusDescription,
                CancellationToken.None
            )
            .ConfigureAwait(false);
    }
}
