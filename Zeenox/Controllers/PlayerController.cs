using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Json;
using Asp.Versioning;
using Discord;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Zeenox.Dtos;
using Zeenox.Enums;
using Zeenox.Models.Player;
using Zeenox.Players;
using Zeenox.Services;

namespace Zeenox.Controllers;

[EnableRateLimiting("global")]
[Authorize]
[ApiController]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class PlayerController(MusicService musicService, DiscordSocketClient client, IAudioService audioService, DatabaseService dbService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPlayer()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (!IsAllowedToPerform(player, user, out var result)) return result;
        var resumeSession = await dbService.GetResumeSessionAsync(player.GuildId).ConfigureAwait(false);
        return Ok(JsonSerializer.Serialize(new FullPlayerDTO(player, resumeSession is null ? null : new ResumeSessionDTO(resumeSession, client))) );
    }

    [Route("options")]
    [HttpGet]
    public async Task<IActionResult> GetPlayer(PayloadType type)
    {
        var data = new Dictionary<string, object?>();
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (!IsAllowedToPerform(player, user, out var result)) return result;
        
        if (type.HasFlag(PayloadType.UpdatePlayer))
        {
            data["State"] = new SocketPlayerDTO(player);
        }
        
        if (type.HasFlag(PayloadType.UpdateQueue))
        {
            data["Queue"] = new QueueDTO(player.Queue);
        }
        
        if (type.HasFlag(PayloadType.UpdateActions))
        {
            data["Actions"] = player.GetActionsForSerialization();
        }
        
        if (type.HasFlag(PayloadType.UpdateTrack))
        {
            data["CurrentTrack"] = player.CurrentItem is not null ? new TrackDTO(player.CurrentItem) : null;
        }
        
        return Ok(JsonSerializer.Serialize(data));
    }
    
    [Route("state")]
    [HttpGet]
    public async Task<IActionResult> GetPlayerState()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        return !IsAllowedToPerform(player, user, out var result) ? result : Ok(JsonSerializer.Serialize(new SocketPlayerDTO(player)));
    }

    [Route("queue")]
    [HttpGet]
    public async Task<IActionResult> GetQueue()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        return !IsAllowedToPerform(player, user, out var result) ? result : Ok(JsonSerializer.Serialize(new QueueDTO(player.Queue)));
    }
    
    [Route("actions")]
    [HttpGet]
    public async Task<IActionResult> GetActions(long? lastActionTimestamp = null)
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        return !IsAllowedToPerform(player, user, out var result) ? result : Ok(JsonSerializer.Serialize(player.GetActionsForSerialization()));
    }
    
    [Route("current")]
    [HttpGet]
    public async Task<IActionResult> GetCurrentTrack()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        
        if (player?.CurrentItem is null)
        {
            return NotFound();
        }
        
        return !IsAllowedToPerform(player, user, out var result) ? result : Ok(JsonSerializer.Serialize(new TrackDTO(player.CurrentItem)));
    }

    [Route("resumesession")]
    [HttpPost]
    public async Task<IActionResult> ResumeSession()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (!IsAllowedToPerform(player, user, out var result)) return result;

        if (!player.HasResumeSession)
        {
            return BadRequest();
        }
        
        await player.ResumeSessionAsync(user).ConfigureAwait(false);
        return Ok();
    }
    
    [Route("resumesession")]
    [HttpDelete]
    public async Task<IActionResult> DeleteResumeSession()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (!IsAllowedToPerform(player, user, out var result)) return result;

        if (!player.HasResumeSession)
        {
            return BadRequest();
        }

        await dbService.DeleteResumeSessionAsync(player.GuildId).ConfigureAwait(false);
        player.RemoveResumeSession();
        return Ok();
    }
    
    [Route("play")]
    [HttpPost]
    public async Task<IActionResult> Play([FromQuery] string url)
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (!IsAllowedToPerform(player, user, out var result)) return result;
        
        var searchResult = await audioService.Tracks.LoadTracksAsync(url, new TrackLoadOptions(TrackSearchMode.None, StrictSearchBehavior.Throw)).ConfigureAwait(false);
        if (!searchResult.IsSuccess)
        {
            return NotFound();
        }

        if (searchResult.IsPlaylist)
        {
            await player.PlayAsync(user, searchResult).ConfigureAwait(false);
        }
        else
        {
            await player.PlayAsync(user, new ExtendedTrackItem(searchResult.Tracks[0], user), false).ConfigureAwait(false);
        }
        
        return Ok();
    }
    
    [Route("add")]
    [HttpPost]
    public async Task<IActionResult> Add([FromQuery] string url)
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (!IsAllowedToPerform(player, user, out var result)) return result;
        
        var track = await audioService.Tracks.LoadTrackAsync(url, new TrackLoadOptions(TrackSearchMode.None, StrictSearchBehavior.Throw)).ConfigureAwait(false);
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
        if (!IsAllowedToPerform(player, user, out var result)) return result;

        await player.PauseAsync(user).ConfigureAwait(false);
        return Ok();
    }

    [Route("resume")]
    [HttpPost]
    public async Task<IActionResult> Resume()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (!IsAllowedToPerform(player, user, out var result)) return result;

        await player.ResumeAsync(user).ConfigureAwait(false);
        return Ok();
    }

    [Route("stop")]
    [HttpPost]
    public async Task<IActionResult> Stop()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (!IsAllowedToPerform(player, user, out var result)) return result;

        await player.StopAsync(user).ConfigureAwait(false);
        return Ok();
    }
    
    [Route("disconnect")]
    [HttpPost]
    public async Task<IActionResult> Disconnect()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (!IsAllowedToPerform(player, user, out var result)) return result;

        await player.DisconnectAsync().ConfigureAwait(false);
        return Ok();
    }

    [Route("move")]
    [HttpPost]
    public async Task<IActionResult> Move([FromQuery] int from, [FromQuery] int to)
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (!IsAllowedToPerform(player, user, out var result)) return result;

        await player.MoveTrackAsync(user, from, to).ConfigureAwait(false);
        return Ok();
    }

    [Route("next")]
    [HttpPost]
    public async Task<IActionResult> Next()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (!IsAllowedToPerform(player, user, out var result)) return result;

        await player.SkipAsync(user).ConfigureAwait(false);
        return Ok();
    }

    [Route("skipto")]
    [HttpPost]
    public async Task<IActionResult> SkipTo([FromQuery] int index)
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (!IsAllowedToPerform(player, user, out var result)) return result;

        await player.SkipToAsync(user, index).ConfigureAwait(false);
        return Ok();
    }

    [Route("removetrack")]
    [HttpPost]
    public async Task<IActionResult> Remove([FromQuery] int index)
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (!IsAllowedToPerform(player, user, out var result)) return result;

        await player.RemoveAtAsync(user, index).ConfigureAwait(false);
        return Ok();
    }

    [Route("rewind")]
    [HttpPost]
    public async Task<IActionResult> Rewind()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (!IsAllowedToPerform(player, user, out var result)) return result;

        await player.RewindAsync(user).ConfigureAwait(false);
        return Ok();
    }

    [Route("seek")]
    [HttpPost]
    public async Task<IActionResult> Seek([FromQuery] int position)
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (!IsAllowedToPerform(player, user, out var result)) return result;

        await player.SeekAsync(user, position).ConfigureAwait(false);
        return Ok();
    }

    [Route("volume")]
    [HttpPost]
    public async Task<IActionResult> SetVolume([FromQuery] int volume)
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (!IsAllowedToPerform(player, user, out var result)) return result;

        await player.SetVolumeAsync(user, volume).ConfigureAwait(false);
        return Ok();
    }

    [Route("repeat")]
    [HttpPost]
    public async Task<IActionResult> Repeat()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (!IsAllowedToPerform(player, user, out var result)) return result;

        await player.CycleRepeatModeAsync(user).ConfigureAwait(false);
        return Ok();
    }

    [Route("shuffle")]
    [HttpPost]
    public async Task<IActionResult> Shuffle()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (!IsAllowedToPerform(player, user, out var result)) return result;
        
        await player.ShuffleAsync(user).ConfigureAwait(false);
        return Ok();
    }

    [Route("distinct")]
    [HttpPost]
    public async Task<IActionResult> DistinctQueue()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (!IsAllowedToPerform(player, user, out var result)) return result;

        await player.DistinctQueueAsync(user).ConfigureAwait(false);
        return Ok();
    }

    [Route("clear")]
    [HttpPost]
    public async Task<IActionResult> ClearQueue()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (!IsAllowedToPerform(player, user, out var result)) return result;

        await player.ClearQueueAsync(user).ConfigureAwait(false);
        return Ok();
    }

    [Route("reverse")]
    [HttpPost]
    public async Task<IActionResult> ReverseQueue()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (!IsAllowedToPerform(player, user, out var result)) return result;

        await player.ReverseQueueAsync(user).ConfigureAwait(false);
        return Ok();
    }

    [Route("autoplay")]
    [HttpPost]
    public async Task<IActionResult> ToggleAutoplay()
    {
        var (user, player) = await GetPlayerAndUserAsync().ConfigureAwait(false);
        if (!IsAllowedToPerform(player, user, out var result)) return result;

        await player.ToggleAutoPlayAsync(user).ConfigureAwait(false);
        return Ok();
    }
    
    private async Task<(IUser?, SocketPlayer?)> GetPlayerAndUserAsync()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        if (!identity.TryGetGuildId(out var guildId) || !identity.TryGetUserId(out var userId))
        {
            return (null, null);
        }

        var player = await musicService.TryGetPlayerAsync(guildId.Value).ConfigureAwait(false);
        return (client.GetUser(userId.Value), player);
    }
    
    private bool IsAllowedToPerform([NotNullWhen(true)] SocketPlayer? player, [NotNullWhen(true)] IUser? user, [NotNullWhen(false)] out IActionResult? result)
    {
        if (player is null || user is null)
        {
            result = NotFound();
            return false;
        }

        if (player.IsUserListening(user))
        {
            result = null;
            return true;
        }
        
        result = Forbid();
        return false;
    }
}