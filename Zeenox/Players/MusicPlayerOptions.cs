using Lavalink4NET;
using Lavalink4NET.Players.Vote;
using SpotifyAPI.Web;

namespace Zeenox.Players;

public record MusicPlayerOptions : VoteLavalinkPlayerOptions
{
    public SpotifyClient SpotifyClient { get; set; } = null!;
    public IAudioService AudioService { get; set; } = null!;
    public string SpotifyMarket { get; set; } = null!;
}