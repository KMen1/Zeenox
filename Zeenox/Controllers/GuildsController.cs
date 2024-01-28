using System.Net.Mime;
using System.Security.Claims;
using Asp.Versioning;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Zeenox.Models;
using Zeenox.Models.Socket;
using Zeenox.Services;

namespace Zeenox.Controllers;

[Authorize]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class GuildsController(DiscordSocketClient client, DatabaseService databaseService) : ControllerBase
{
    [Route("available")]
    [HttpGet]
    public async Task<IActionResult> GetAvailableGuilds(bool includeResumeSessions = false)
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var userId = identity!.GetUserId();

        var guilds = client.Guilds.Where(
            x =>
                x.Users.Select(y => y.Id).Contains(userId!.Value)
            //&& x.Users.First(z => z.Id == userId).GuildPermissions.ManageGuild
        ).ToList();

        if (!includeResumeSessions)
        {
            var guildsList = guilds.Select(x => new BasicDiscordGuild(x, null));
            return Content(JsonConvert.SerializeObject(guildsList));
        }
        
        var resumeSessions = await databaseService.GetResumeSessionsAsync(guilds.Select(x => x.Id).ToArray()).ConfigureAwait(false);
        var guildsListWithResumeSessions = guilds.Select(x => new BasicDiscordGuild(x, PlayerResumeSessionDto.Create(resumeSessions.FirstOrDefault(y => y.GuildId == x.Id), client)));
        return Content(JsonConvert.SerializeObject(guildsListWithResumeSessions));
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
        var resumeSessionDto = resumeSession is null ? null : new PlayerResumeSessionDto(resumeSession, client);
        return Ok(JsonConvert.SerializeObject(new BasicDiscordGuild(guild, resumeSessionDto)));
    }
}