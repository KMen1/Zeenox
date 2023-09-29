using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Zeenox.Services;
using SocketMessage = Zeenox.Models.SocketMessage;

namespace Zeenox.Controllers;

[Authorize]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]/[action]")]
[ApiVersion("1.0")]
public class PlayerController : ControllerBase
{
    private readonly MusicService _musicService;
    private readonly DatabaseService _databaseService;

    public PlayerController(MusicService musicService, DatabaseService databaseService)
    {
        _musicService = musicService;
        _databaseService = databaseService;
    }

    [HttpGet(Name = "GetPlayer")]
    public async Task<IActionResult> GetPlayer()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var guildId = ulong.Parse(identity!.FindFirst("guildId")!.Value);

        var player = await _musicService.TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
        {
            return Content(
                JsonConvert.SerializeObject(SocketMessage.Empty),
                "application/json"
            );
        }

        return Content(
            JsonConvert.SerializeObject(SocketMessage.FromZeenoxPlayer(player, true, true, true)),
            "application/json"
        );
    }

    [HttpGet]
    public async Task<IActionResult> GetFavoriteTracks()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var userId = ulong.Parse(identity!.FindFirst("userId")!.Value);

        var user = await _databaseService.GetUserAsync(userId).ConfigureAwait(false);
        return Ok(user.FavoriteSongs);
    }

    [HttpGet]
    public async Task<IActionResult> GetLyrics()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var guildId = ulong.Parse(identity!.FindFirst("guildId")!.Value);

        var lyrics = await _musicService.GetLyricsAsync(guildId).ConfigureAwait(false);
        return lyrics is null ? NotFound() : Ok(lyrics);
    }

    [HttpPost]
    public async Task<IActionResult> Pause()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var guildId = ulong.Parse(identity!.FindFirst("guildId")!.Value);

        try
        {
            await _musicService.PauseOrResumeAsync(guildId).ConfigureAwait(false);
            await _musicService
                .UpdateSocketsAsync(guildId, updatePlayer: true)
                .ConfigureAwait(false);
            return Ok();
        }
        catch (Exception e)
        {
            return Problem(e.StackTrace, e.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Resume()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var guildId = ulong.Parse(identity!.FindFirst("guildId")!.Value);

        try
        {
            await _musicService.PauseOrResumeAsync(guildId).ConfigureAwait(false);
            await _musicService
                .UpdateSocketsAsync(guildId, updatePlayer: true)
                .ConfigureAwait(false);
            return Ok();
        }
        catch (Exception e)
        {
            return Problem(e.StackTrace, e.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Stop()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var guildId = ulong.Parse(identity!.FindFirst("guildId")!.Value);

        var player = await _musicService.TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }

        await player.StopAsync().ConfigureAwait(false);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Move(int from, int to)
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var guildId = ulong.Parse(identity!.FindFirst("guildId")!.Value);

        var player = await _musicService.TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }

        await player.MoveTrackAsync(from, to).ConfigureAwait(false);
        await _musicService.UpdateSocketsAsync(guildId, updateQueue: true).ConfigureAwait(false);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Next()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var guildId = ulong.Parse(identity!.FindFirst("guildId")!.Value);

        var player = await _musicService.TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }

        await player.SkipAsync().ConfigureAwait(false);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> SkipTo(int index)
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var guildId = ulong.Parse(identity!.FindFirst("guildId")!.Value);

        var player = await _musicService.TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }

        await player.SkipToAsync(index).ConfigureAwait(false);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Remove(int index)
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var guildId = ulong.Parse(identity!.FindFirst("guildId")!.Value);

        var player = await _musicService.TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }

        await player.RemoveAsync(index).ConfigureAwait(false);
        return Content(
            JsonConvert.SerializeObject(SocketMessage.FromZeenoxPlayer(player, updateQueue: true)),
            "application/json"
        );
    }

    [HttpPost]
    public async Task<IActionResult> Like(int index)
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var guildId = ulong.Parse(identity!.FindFirst("guildId")!.Value);
        var userId = ulong.Parse(identity!.FindFirst("userId")!.Value);

        var player = await _musicService.TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }

        var track = index == 0 ? player.CurrentItem : player.Queue.ElementAt(index);
        if (track is null)
            return NotFound();

        await _databaseService
            .UpdateUserAsync(
                userId,
                x =>
                {
                    if (x.FavoriteSongs.Contains(track.Track!.ToString()))
                    {
                        x.FavoriteSongs.Remove(track.Track.ToString());
                    }
                    else
                    {
                        x.FavoriteSongs.Add(track.Track.ToString());
                    }
                }
            )
            .ConfigureAwait(false);

        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Back()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var guildId = ulong.Parse(identity!.FindFirst("guildId")!.Value);

        var player = await _musicService.TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }

        await player.RewindAsync().ConfigureAwait(false);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Seek(int position)
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var guildId = ulong.Parse(identity!.FindFirst("guildId")!.Value);

        try
        {
            await _musicService.SeekAsync(guildId, position).ConfigureAwait(false);
            return Ok();
        }
        catch (Exception e)
        {
            return Problem(e.StackTrace, e.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> SetVolume(int volume)
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var guildId = ulong.Parse(identity!.FindFirst("guildId")!.Value);

        try
        {
            await _musicService.SetVolumeAsync(guildId, volume).ConfigureAwait(false);
            await _musicService
                .UpdateSocketsAsync(guildId, updatePlayer: true)
                .ConfigureAwait(false);
            return Ok();
        }
        catch (Exception e)
        {
            return Problem(e.StackTrace, e.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Repeat()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var guildId = ulong.Parse(identity!.FindFirst("guildId")!.Value);

        try
        {
            await _musicService.CycleLoopModeAsync(guildId).ConfigureAwait(false);
            await _musicService
                .UpdateSocketsAsync(guildId, updatePlayer: true)
                .ConfigureAwait(false);
            return Ok();
        }
        catch (Exception e)
        {
            return Problem(e.StackTrace, e.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Shuffle()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var guildId = ulong.Parse(identity!.FindFirst("guildId")!.Value);

        var player = await _musicService.TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }

        await player.ShuffleAsync().ConfigureAwait(false);
        await _musicService.UpdateSocketsAsync(guildId, updateQueue: true).ConfigureAwait(false);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> DistinctQueue()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var guildId = ulong.Parse(identity!.FindFirst("guildId")!.Value);

        var player = await _musicService.TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }

        await player.DistinctQueueAsync().ConfigureAwait(false);
        await _musicService.UpdateSocketsAsync(guildId, updateQueue: true).ConfigureAwait(false);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> ClearQueue()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var guildId = ulong.Parse(identity!.FindFirst("guildId")!.Value);

        var player = await _musicService.TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }

        await player.ClearQueueAsync().ConfigureAwait(false);
        await _musicService.UpdateSocketsAsync(guildId, updateQueue: true).ConfigureAwait(false);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> ReverseQueue()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        var guildId = ulong.Parse(identity!.FindFirst("guildId")!.Value);

        var player = await _musicService.TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }

        await player.ShuffleAsync().ConfigureAwait(false);
        await _musicService.UpdateSocketsAsync(guildId, updateQueue: true).ConfigureAwait(false);
        return Ok();
    }
}
