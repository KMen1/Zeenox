﻿using Microsoft.AspNetCore.Mvc;
using Zeenox.Services;

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

    [HttpGet(Name = "GetPlayerInfo")]
    public async Task<IActionResult> GetPlayerInfo(ulong guildId)
    {
        var (playerExists, player) = await _musicService.TryGetPlayer(guildId).ConfigureAwait(false);
        if (!playerExists)
        {
            return NotFound();
        }
        return Content(player!.ToJson(), "application/json");
    }

    [HttpGet(Name = "GetFavoriteTracks")]
    public async Task<IActionResult> GetFavoriteTracks(ulong userId)
    {
        var user = await _databaseService.GetUserAsync(userId).ConfigureAwait(false);
        return Ok(user.FavoriteSongs);
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
        try
        {
            return Ok("Stopped");
        }
        catch (Exception e)
        {
            return Problem(e.StackTrace, e.Message);
        }
    }

    [HttpPost(Name = "Skip")]
    public async Task<IActionResult> Skip(ulong guildId, ulong userId)
    {
        try
        {
            await _musicService.SkipAsync(guildId).ConfigureAwait(false);
            return Ok();
        }
        catch (Exception e)
        {
            return Problem(e.StackTrace, e.Message);
        }
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

    [HttpPost(Name = "CycleLoopMode")]
    public async Task<IActionResult> CycleLoopMode(ulong guildId, ulong userId)
    {
        try
        {
            await _musicService.CycleLoopMode(guildId).ConfigureAwait(false);
            return Ok();
        }
        catch (Exception e)
        {
            return Problem(e.StackTrace, e.Message);
        }
    }

    [HttpPost(Name = "ShuffleQueue")]
    public async Task<IActionResult> ShuffleQueue(ulong guildId, ulong userId)
    {
        try
        {
            await _musicService.ShuffleQueueAsync(guildId).ConfigureAwait(false);
            return Ok();
        }
        catch (Exception e)
        {
            return Problem(e.StackTrace, e.Message);
        }
    }
}
