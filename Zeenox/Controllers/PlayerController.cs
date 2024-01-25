using System.Security.Claims;
using Asp.Versioning;
using Discord;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Players;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zeenox.Models.Player;
using Zeenox.Players;
using Zeenox.Services;

namespace Zeenox.Controllers;

[Authorize]
[ApiController]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class PlayerController(MusicService musicService, DiscordSocketClient client, IAudioService audioService, DatabaseService dbService) : ControllerBase
{
    private async Task<(IUser, LoggedPlayer?)> GetPlayerAndUserAsync()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var guildId = identity?.GetGuildId();
        var userId = identity?.GetUserId();

        var player = await musicService.TryGetPlayerAsync(guildId.GetValueOrDefault()).ConfigureAwait(false);
        return (client.GetUser(userId!.Value), player);
    }
    
    [Route("lyrics")]
    [HttpGet]
    public async Task<IActionResult> GetLyrics()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var guildId = ulong.Parse(identity!.FindFirst("GUILD_ID")!.Value);

        var lyrics = await musicService.GetLyricsAsync(guildId).ConfigureAwait(false);
        return lyrics is null ? NotFound() : Ok(lyrics);
    }

    [Route("resumesession")]
    [HttpPost]
    public async Task<IActionResult> ResumeSession()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }
        if (!player.IsUserListening(user))
        {
            return Forbid();
        }
        
        var resumeSession = await dbService.GetResumeSessionAsync(player.GuildId).ConfigureAwait(false);
        if (resumeSession is null)
        {
            throw new Exception("Resume session is null");
        }

        await player.ResumeSessionAsync(user, client).ConfigureAwait(false);
        return Ok();
    }
    
    [Route("play")]
    [HttpPost]
    public async Task<IActionResult> Play([FromQuery] string url)
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }
        if (!player.IsUserListening(user))
        {
            return Forbid();
        }
        
        var result = await audioService.Tracks.LoadTracksAsync(url, new TrackLoadOptions { SearchMode = TrackSearchMode.None, StrictSearch = true }).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return NotFound();
        }

        if (result.IsPlaylist)
        {
            await player.PlayAsync(user, result).ConfigureAwait(false);
            return Ok();
        }
        
        await player.PlayAsync(user, new ExtendedTrackItem(result.Tracks[0], user), false).ConfigureAwait(false);
        return Ok();
    }
    
    [Route("add")]
    [HttpPost]
    public async Task<IActionResult> Add([FromQuery] string url)
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }
        if (!player.IsUserListening(user))
        {
            return Forbid();
        }
        
        var track = await audioService.Tracks.LoadTrackAsync(url, new TrackLoadOptions { SearchMode = TrackSearchMode.None, StrictSearch = true }).ConfigureAwait(false);
        if (track is null)
        {
            return NotFound();
        }
        
        await player.PlayAsync(user, new ExtendedTrackItem(track, user)).ConfigureAwait(false);
        return Ok();
    }

    [Route("pause")]
    [HttpPost]
    public async Task<IActionResult> Pause()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }
        if (!player.IsUserListening(user))
        {
            return Forbid();
        }

        await player.PauseAsync(user).ConfigureAwait(false);
        return Ok();
    }

    [Route("resume")]
    [HttpPost]
    public async Task<IActionResult> Resume()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }
        if (!player.IsUserListening(user))
        {
            return Forbid();
        }

        await player.ResumeAsync(user).ConfigureAwait(false);
        return Ok();
    }

    [Route("stop")]
    [HttpPost]
    public async Task<IActionResult> Stop()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }
        if (!player.IsUserListening(user))
        {
            return Forbid();
        }

        await player.StopAsync(user).ConfigureAwait(false);
        return Ok();
    }
    
    [Route("disconnect")]
    [HttpPost]
    public async Task<IActionResult> Disconnect()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }
        if (!player.IsUserListening(user))
        {
            return Forbid();
        }

        await player.DisconnectAsync().ConfigureAwait(false);
        return Ok();
    }

    [Route("move")]
    [HttpPost]
    public async Task<IActionResult> Move([FromQuery] int from, [FromQuery] int to)
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }
        if (!player.IsUserListening(user))
        {
            return Forbid();
        }

        await player.MoveTrackAsync(user, from, to).ConfigureAwait(false);
        return Ok();
    }

    [Route("next")]
    [HttpPost]
    public async Task<IActionResult> Next()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }
        if (!player.IsUserListening(user))
        {
            return Forbid();
        }

        await player.SkipAsync(user).ConfigureAwait(false);
        return Ok();
    }

    [Route("skipto")]
    [HttpPost]
    public async Task<IActionResult> SkipTo([FromQuery] int index)
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }
        if (!player.IsUserListening(user))
        {
            return Forbid();
        }

        await player.SkipToAsync(user, index).ConfigureAwait(false);
        return Ok();
    }

    [Route("removetrack")]
    [HttpPost]
    public async Task<IActionResult> Remove([FromQuery] int index)
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }
        if (!player.IsUserListening(user))
        {
            return Forbid();
        }

        await player.RemoveAtAsync(user, index).ConfigureAwait(false);
        return Ok();
    }

    [Route("rewind")]
    [HttpPost]
    public async Task<IActionResult> Rewind()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }
        if (!player.IsUserListening(user))
        {
            return Forbid();
        }

        await player.RewindAsync(user).ConfigureAwait(false);
        return Ok();
    }

    [Route("seek")]
    [HttpPost]
    public async Task<IActionResult> Seek([FromQuery] int position)
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }
        if (!player.IsUserListening(user))
        {
            return Forbid();
        }

        await player.SeekAsync(user, position).ConfigureAwait(false);
        return Ok();
    }

    [Route("volume")]
    [HttpPost]
    public async Task<IActionResult> SetVolume([FromQuery] int volume)
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }
        if (!player.IsUserListening(user))
        {
            return Forbid();
        }

        await player.SetVolumeAsync(user, volume).ConfigureAwait(false);
        return Ok();
    }

    [Route("repeat")]
    [HttpPost]
    public async Task<IActionResult> Repeat()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }
        if (!player.IsUserListening(user))
        {
            return Forbid();
        }

        await player.CycleRepeatModeAsync(user).ConfigureAwait(false);
        return Ok();
    }

    [Route("shuffle")]
    [HttpPost]
    public async Task<IActionResult> Shuffle()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }
        if (!player.IsUserListening(user))
        {
            return Forbid();
        }

        
        await player.ShuffleAsync(user).ConfigureAwait(false);
        return Ok();
    }

    [Route("distinct")]
    [HttpPost]
    public async Task<IActionResult> DistinctQueue()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }
        if (!player.IsUserListening(user))
        {
            return Forbid();
        }

        await player.DistinctQueueAsync(user).ConfigureAwait(false);
        return Ok();
    }

    [Route("clear")]
    [HttpPost]
    public async Task<IActionResult> ClearQueue()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }
        if (!player.IsUserListening(user))
        {
            return Forbid();
        }

        await player.ClearQueueAsync(user).ConfigureAwait(false);
        return Ok();
    }

    [Route("reverse")]
    [HttpPost]
    public async Task<IActionResult> ReverseQueue()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }
        if (!player.IsUserListening(user))
        {
            return Forbid();
        }

        await player.ReverseQueueAsync(user).ConfigureAwait(false);
        return Ok();
    }
}