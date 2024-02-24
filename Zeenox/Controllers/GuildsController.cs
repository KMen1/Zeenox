using System.Net.Mime;
using System.Security.Claims;
using Asp.Versioning;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Zeenox.Dtos;
using Zeenox.Services;

namespace Zeenox.Controllers;

[Authorize]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class GuildsController(DiscordSocketClient client, DatabaseService databaseService, MusicService musicService) : ControllerBase
{
    [Route("available")]
    [HttpGet]
    public async Task<IActionResult> GetAvailableGuilds()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        if (!identity.TryGetUserId(out var userId))
        {
            return BadRequest();
        }

        var guilds = client.Guilds.Where(
            x =>
                x.Users.Select(y => y.Id).Contains(userId.Value)
            //&& x.Users.First(z => z.Id == userId).GuildPermissions.ManageGuild
        ).ToList();



        var guildsList = new List<SocketGuildDTO>();

        foreach (var guild in guilds)
        {
            var voiceChannel = guild.VoiceChannels.FirstOrDefault(x => x.ConnectedUsers.Any(y => y.Id == client.CurrentUser.Id));
            var resumeSession = await databaseService.GetResumeSessionAsync(guild.Id).ConfigureAwait(false);
            var player = await musicService.TryGetPlayerAsync(guild.Id).ConfigureAwait(false);
            var trackDto = player?.CurrentItem is not null ? new TrackDTO(player.CurrentItem) : null;
            var resumeSessionDto = resumeSession is not null ? new ResumeSessionDTO(resumeSession, client) : null;
            guildsList.Add(new SocketGuildDTO(guild, trackDto, voiceChannel?.Name, resumeSessionDto));
        }

        return Content(JsonConvert.SerializeObject(guildsList));
    }

    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpGet]
    public async Task<IActionResult> GetGuild([FromQuery] ulong id)
    {
        var guild = client.Guilds.FirstOrDefault(x => x.Id == id);
        if (guild is null)
        {
            return NotFound();
        }

        var resumeSession = await databaseService.GetResumeSessionAsync(id).ConfigureAwait(false);
        var voiceChannel = guild.VoiceChannels.FirstOrDefault(x => x.ConnectedUsers.Any(y => y.Id == client.CurrentUser.Id));
        var resumeSessionDto = resumeSession is null ? null : new ResumeSessionDTO(resumeSession, client);
        var player = await musicService.TryGetPlayerAsync(guild.Id).ConfigureAwait(false);
        var trackDto = player?.CurrentItem is not null ? new TrackDTO(player.CurrentItem) : null;
        return Ok(JsonConvert.SerializeObject(new SocketGuildDTO(guild, trackDto, voiceChannel?.Name, resumeSessionDto)));
    }
}