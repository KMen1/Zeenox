using Lavalink4NET;
using Lavalink4NET.Players.Vote;

namespace Zeenox.Players;

public record MusicPlayerOptions : VoteLavalinkPlayerOptions
{
    public IAudioService AudioService { get; set; } = null!;
}
