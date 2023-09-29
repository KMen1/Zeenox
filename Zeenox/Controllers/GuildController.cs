using System.Security.Claims;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Zeenox.Models;

namespace Zeenox.Controllers;

[Authorize]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]/[action]")]
[ApiVersion("1.0")]
public class GuildController : ControllerBase
{
    private readonly DiscordSocketClient  _client;

    public GuildController(DiscordSocketClient client)
    {
        _client = client;
    }
    
    [HttpGet(Name = "GetAvailableGuilds")]
    public IActionResult GetAvailableGuilds()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var userId = ulong.Parse(identity!.FindFirst("userId")!.Value);
        
        var guilds = _client.Guilds.Where(
            x =>
                x.Users.Select(y => y.Id).Contains(userId)
                //&& x.Users.First(z => z.Id == userId).GuildPermissions.ManageGuild
        );
        var guildsList = guilds.Select(GuildInfo.FromSocketGuild);
        return Ok(JsonConvert.SerializeObject(guildsList));
    }
    
    [HttpGet(Name = "GetGuild")]
    public IActionResult GetGuild([FromQuery] ulong id)
    {
        var guild = _client.Guilds.FirstOrDefault(x => x.Id == id);
        if (guild is null)
        {
            return NotFound();
        }

        return Ok(JsonConvert.SerializeObject(GuildInfo.FromSocketGuild(guild)));
    }
}