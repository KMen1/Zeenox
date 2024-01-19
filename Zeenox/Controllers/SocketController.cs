﻿using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using Asp.Versioning;
using Discord.WebSocket;
using Lavalink4NET.Players;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Zeenox.Models;
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
    private readonly DatabaseService _dbService;
    private static string _jwtsecret = "";

    public SocketController(
        MusicService musicService,
        IConfiguration configuration,
        DiscordSocketClient client, DatabaseService dbService)
    {
        _musicService = musicService;
        _client = client;
        _dbService = dbService;
        _jwtsecret = configuration["JwtSettings:Key"]!;
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

    private static ClaimsPrincipal? GetClaims(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var validations = new TokenValidationParameters
        {
            ValidIssuer = "https://zeenox.tech",
            ValidAudience = "https://zeenox-web.vercel.app",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtsecret)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true
        };
        return handler.ValidateToken(jwt, validations, out _);
    }

    private async Task HandleSocketAsync(WebSocket socket)
    {
        var buffer = new byte[1024 * 4];
        var receiveResult = await socket
            .ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None)
            .ConfigureAwait(false);

        var jwt = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
        if (string.IsNullOrWhiteSpace(jwt))
            return;

        var claims = GetClaims(jwt);
        if (claims is null)
            return;

        var guildId = claims.GetGuildId();
        var userId = claims.GetUserId();

        if (guildId is null || userId is null)
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

        player.AddSocket(userId.Value, socket);
        var resume = await _dbService
            .GetResumeSessionAsync(guildId.Value)
            .ConfigureAwait(false);
        var t = PlayerResumeSessionDto.Create(resume, _client);
        await socket.InitSocketAsync(player, t).ConfigureAwait(false);

        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        await using var ctr = token
            .Register(() => player.RemoveSocket(userId.Value))
            .ConfigureAwait(false);

        var sendTask = SendPositionLoopAsync(socket, guildId.Value, token);
        var receiveTask = ReceiveLoopAsync(socket, cts);
        await Task.WhenAll(receiveTask, sendTask).ConfigureAwait(false);
    }

    private async Task SendPositionLoopAsync(
        WebSocket socket,
        ulong guildId,
        CancellationToken token
    )
    {
        while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            var player = await _musicService.TryGetPlayerAsync(guildId).ConfigureAwait(false);
            if (player?.State is PlayerState.Playing)
            {
                await socket
                    .SendSocketMessagesAsync(player, true, false, false, false)
                    .ConfigureAwait(false);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
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

        await cts.CancelAsync().ConfigureAwait(false);

        await socket
            .CloseAsync(
                receiveResult.CloseStatus.Value,
                receiveResult.CloseStatusDescription,
                CancellationToken.None
            )
            .ConfigureAwait(false);
    }
}
