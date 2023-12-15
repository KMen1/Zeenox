using System.Net.Mime;
using System.Security.Claims;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Zeenox.Models.Socket;

namespace Zeenox.Controllers;

[Authorize]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class GuildsController(DiscordSocketClient client) : ControllerBase
{
    [Route("available")]
    [HttpGet]
    public IActionResult GetAvailableGuilds()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var userId = identity!.GetUserId();

        var guilds = client.Guilds.Where(
            x =>
                x.Users.Select(y => y.Id).Contains(userId!.Value)
            //&& x.Users.First(z => z.Id == userId).GuildPermissions.ManageGuild
        );
        var guildsList = guilds.Select(x => new GuildDto(x));

        return Content(JsonConvert.SerializeObject(guildsList));
    }

    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpGet]
    public IActionResult GetGuild([FromQuery] ulong id)
    {
        var guild = client.Guilds.FirstOrDefault(x => x.Id == id);
        if (guild is null)
        {
            return NotFound();
        }

        return Ok(JsonConvert.SerializeObject(new GuildDto(guild)));
    }
}