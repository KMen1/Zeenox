using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Zeenox.Models;
using Zeenox.Services;
using SocketMessage = Zeenox.Models.SocketMessage;

namespace Zeenox.Controllers;

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
    public async Task<IActionResult> GetPlayer(ulong guildId)
    {
        var player = await _musicService.TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }

        return Content(
            JsonConvert.SerializeObject(SocketMessage.FromZeenoxPlayer(player, true, true, true)),
            "application/json"
        );
    }

    [HttpGet(Name = "GetFavoriteTracks")]
    public async Task<IActionResult> GetFavoriteTracks(ulong userId)
    {
        var user = await _databaseService.GetUserAsync(userId).ConfigureAwait(false);
        return Ok(user.FavoriteSongs);
    }

    [HttpGet(Name = "GetLyrics")]
    public async Task<IActionResult> GetLyrics(ulong guildId)
    {
        var lyrics = await _musicService.GetLyricsAsync(guildId).ConfigureAwait(false);
        return lyrics is null ? NotFound() : Ok(lyrics);
    }

    [HttpPost(Name = "Pause")]
    public async Task<IActionResult> Pause(ulong guildId, ulong userId)
    {
        try
        {
            await _musicService.PauseOrResumeAsync(guildId).ConfigureAwait(false);
            return Ok();
        }
        catch (Exception e)
        {
            return Problem(e.StackTrace, e.Message);
        }
    }

    [HttpPost(Name = "Resume")]
    public async Task<IActionResult> Resume(ulong guildId, ulong userId)
    {
        try
        {
            await _musicService.PauseOrResumeAsync(guildId).ConfigureAwait(false);
            return Ok();
        }
        catch (Exception e)
        {
            return Problem(e.StackTrace, e.Message);
        }
    }

    [HttpPost(Name = "Stop")]
    public async Task<IActionResult> Stop(ulong guildId, ulong userId)
    {
        var player = await _musicService.TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }

        await player.StopAsync().ConfigureAwait(false);
        return Ok();
    }

    [HttpPost(Name = "Next")]
    public async Task<IActionResult> Next(ulong guildId, ulong userId)
    {
        var player = await _musicService.TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }

        await player.SkipAsync().ConfigureAwait(false);
        return Ok();
    }

    [HttpPost(Name = "Back")]
    public async Task<IActionResult> Back(ulong guildId, ulong userId)
    {
        var player = await _musicService.TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }

        await player.RewindAsync().ConfigureAwait(false);
        return Ok();
    }

    [HttpPost(Name = "Seek")]
    public async Task<IActionResult> Seek(ulong guildId, ulong userId, int position)
    {
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

    [HttpPost(Name = "SetVolume")]
    public async Task<IActionResult> SetVolume(ulong guildId, ulong userId, int volume)
    {
        try
        {
            await _musicService.SetVolumeAsync(guildId, volume).ConfigureAwait(false);
            return Ok();
        }
        catch (Exception e)
        {
            return Problem(e.StackTrace, e.Message);
        }
    }

    [HttpPost(Name = "Repeat")]
    public async Task<IActionResult> Repeat(ulong guildId, ulong userId)
    {
        try
        {
            await _musicService.CycleLoopModeAsync(guildId).ConfigureAwait(false);
            return Ok();
        }
        catch (Exception e)
        {
            return Problem(e.StackTrace, e.Message);
        }
    }

    [HttpPost(Name = "Shuffle")]
    public async Task<IActionResult> Shuffle(ulong guildId, ulong userId)
    {
        var player = await _musicService.TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }

        await player.ShuffleAsync().ConfigureAwait(false);
        return Ok();
    }

    [HttpPost(Name = "DistinctQueue")]
    public async Task<IActionResult> DistinctQueue(ulong guildId, ulong userId)
    {
        var player = await _musicService.TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }

        await player.DistinctQueueAsync().ConfigureAwait(false);
        return Ok();
    }

    [HttpPost(Name = "ClearQueue")]
    public async Task<IActionResult> ClearQueue(ulong guildId, ulong userId)
    {
        var player = await _musicService.TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }

        await player.ClearQueueAsync().ConfigureAwait(false);
        return Ok();
    }

    [HttpPost(Name = "ReverseQueue")]
    public async Task<IActionResult> ReverseQueue(ulong guildId, ulong userId)
    {
        var player = await _musicService.TryGetPlayerAsync(guildId).ConfigureAwait(false);
        if (player is null)
        {
            return NotFound();
        }

        await player.ShuffleAsync().ConfigureAwait(false);
        return Ok();
    }
}
