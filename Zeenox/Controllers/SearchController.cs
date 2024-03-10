using Asp.Versioning;
using Lavalink4NET;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Zeenox.Dtos;

namespace Zeenox.Controllers;

[Authorize]
[ApiController]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class SearchController(IAudioService audioService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> SearchAsync(string query)
    {
        var result = await audioService.Tracks
                                       .LoadTracksAsync(
                                           query,
                                           new TrackLoadOptions(TrackSearchMode.Spotify, StrictSearchBehavior.Throw))
                                       .ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return NotFound();
        }

        if (!result.HasMatches)
        {
            return NotFound();
        }

        return Content(JsonConvert.SerializeObject(new SearchResultDTO(result)));
    }
}