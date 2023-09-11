using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Lavalink4NET.Players;
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

    [HttpGet(Name = "Connect")]
    public async Task Connect(ulong guildId, ulong userId)
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
        _musicService.AddWebSocket(guildId, socket);

        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        await using var ctr = token
            .Register(() => _musicService.RemoveWebSocket(guildId, socket))
            .ConfigureAwait(false);

        var sendTask = SendLoopAsync(socket, guildId, token);
        var receiveTask = ReceiveLoopAsync(socket, cts);
        await Task.WhenAll(receiveTask, sendTask).ConfigureAwait(false);
    }

    private async Task SendLoopAsync(WebSocket socket, ulong guildId, CancellationToken token)
    {
        var player = await _musicService.TryGetPlayerAsync(guildId).ConfigureAwait(false);
        while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            if (player?.State is PlayerState.Playing)
            {
                var message = SocketMessage.FromZeenoxPlayer(player, updatePlayer: true);
                await socket
                    .SendAsync(
                        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    )
                    .ConfigureAwait(false);
            }

            try
            {
                await Task.Delay(1000, token).ConfigureAwait(false);
            }
            catch (TaskCanceledException e)
            {
                break;
            }
        }
    }

    private static async Task ReceiveLoopAsync(WebSocket socket, CancellationTokenSource cts)
    {
        var buffer = new byte[1024 * 4];
        var receiveResult = await socket
            .ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None)
            .ConfigureAwait(false);

        while (!receiveResult.CloseStatus.HasValue)
        {
            receiveResult = await socket
                .ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None)
                .ConfigureAwait(false);
        }

        cts.Cancel();

        await socket
            .CloseAsync(
                receiveResult.CloseStatus.Value,
                receiveResult.CloseStatusDescription,
                CancellationToken.None
            )
            .ConfigureAwait(false);
    }
}
