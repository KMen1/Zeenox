using Microsoft.Extensions.Caching.Memory;
using SpotifyAPI.Web;

namespace Zeenox.Services;

public class SpotifyService
{
    private readonly SpotifyClient _spotifyClient;
    private readonly IMemoryCache _memoryCache;

    public SpotifyService(SpotifyClient spotifyClient, IMemoryCache memoryCache)
    {
        _spotifyClient = spotifyClient;
        _memoryCache = memoryCache;
    }

    public async Task<string?> GetCoverUrl(string id)
    {
        if (_memoryCache.TryGetValue(id, out string? coverUrl))
            return coverUrl;
        var track = await _spotifyClient.Tracks.Get(id).ConfigureAwait(false);
        coverUrl = track.Album.Images[0].Url;
        _memoryCache.Set(id, coverUrl, TimeSpan.FromMinutes(5));

        return coverUrl;
    }
}
