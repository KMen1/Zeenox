using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using Asp.Versioning;
using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Zeenox.Services;

namespace Zeenox.Controllers;

[ApiController]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class SocketController : ControllerBase
{
    private readonly MusicService _musicService;
    private readonly DiscordSocketClient _client;
    private static SymmetricSecurityKey _securityKey = null!;

    public SocketController(
        MusicService musicService,
        IConfiguration configuration,
        DiscordSocketClient client
    )
    {
        _musicService = musicService;
        _client = client;
        _securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                configuration["JwtSettings:Key"] ?? throw new Exception("JWT key is not set!")
            )
        );
    }

    [HttpGet]
    public async Task Connect()
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var webSocket = await HttpContext.WebSockets
            .AcceptWebSocketAsync()
            .ConfigureAwait(false);
        await HandleSocketAsync(webSocket).ConfigureAwait(false);
    }

    private async Task HandleSocketAsync(WebSocket socket)
    {
        if (socket.CloseStatus.HasValue)
            return;

        var buffer = new byte[1024 * 4];
        var receiveResult = await socket
            .ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None)
            .ConfigureAwait(false);

        var jwt = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
        if (string.IsNullOrWhiteSpace(jwt))
            return;

        if (!TryGetClaims(jwt, out var claims))
            return;

        if (!claims.TryGetGuildId(out var guildId) || !claims.TryGetUserId(out var userId))
            return;

        var player = await _musicService.TryGetPlayerAsync(guildId.Value).ConfigureAwait(false);
        if (player is null)
        {
            var user = _client.GetGuild(guildId.Value).GetUser(userId.Value);
            var voiceChannel = user.VoiceChannel;
            if (voiceChannel is null)
                return;

            player = await _musicService
                .TryCreatePlayerAsync(guildId.Value, voiceChannel)
                .ConfigureAwait(false);
            if (player is null)
                return;
        }

        await player.RegisterSocketAsync(userId.Value, socket).ConfigureAwait(false);
        await ReceiveAsync(socket).ConfigureAwait(false);
    }

    private static async Task ReceiveAsync(WebSocket socket)
    {
        var buffer = new byte[1024 * 4];
        var result = await socket
            .ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None)
            .ConfigureAwait(false);

        while (!result.CloseStatus.HasValue)
        {
            result = await socket
                .ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None)
                .ConfigureAwait(false);
        }

        if (
            result is { MessageType: WebSocketMessageType.Close, CloseStatus: not null }
            && socket.State == WebSocketState.Open
        )
        {
            await socket
                .CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closed by the client",
                    CancellationToken.None
                )
                .ConfigureAwait(false);
        }
    }

    private static bool TryGetClaims(string jwt, out ClaimsPrincipal? claims)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var validations = new TokenValidationParameters
            {
                ValidIssuer = "https://zeenox.tech",
                ValidAudience = "https://zeenox-web.vercel.app",
                IssuerSigningKey = _securityKey,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true
            };
            claims = handler.ValidateToken(jwt, validations, out _);
            return claims is not null;
        }
        catch
        {
            claims = null;
            return false;
        }
    }
}
