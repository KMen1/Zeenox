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
    public IActionResult GetPlayerInfo(ulong guildId)
    {
        return Ok("Player");
    }

    [HttpGet("GetFavoriteTracks")]
    public async Task<IActionResult> GetFavoriteTracks(ulong userId)
    {
        var user = await _databaseService.GetUserAsync(userId);
        return Ok(user.FavoriteSongs);
    }

    [HttpPost(Name = "Pause")]
    public async Task<IActionResult> Pause(ulong guildId, ulong userId)
    {
        try
        {
            await _musicService.PauseOrResumeAsync(guildId);
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
            await _musicService.PauseOrResumeAsync(guildId);
            return Ok();
        }
        catch (Exception e)
        {
            return Problem(e.StackTrace, e.Message);
        }
    }

    [HttpPost(Name = "Stop")]
    public IActionResult Stop(ulong guildId, ulong userId)
    {
        return Ok("Stopped");
    }

    [HttpPost(Name = "Skip")]
    public IActionResult Skip(ulong guildId, ulong userId)
    {
        return Ok("Skipped");
    }

    [HttpPost(Name = "Seek")]
    public async Task<IActionResult> Seek(ulong guildId, ulong userId, int position)
    {
        try
        {
            await _musicService.SeekAsync(guildId, position);
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
            await _musicService.SetVolumeAsync(guildId, volume);
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
            await _musicService.CycleLoopMode(guildId);
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
            await _musicService.ShuffleQueueAsync(guildId);
            return Ok();
        }
        catch (Exception e)
        {
            return Problem(e.StackTrace, e.Message);
        }
    }
}
