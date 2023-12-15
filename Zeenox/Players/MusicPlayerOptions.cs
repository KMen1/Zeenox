using Lavalink4NET.Players.Vote;

namespace Zeenox.Players;

public record MusicPlayerOptions : VoteLavalinkPlayerOptions
{
    public DateTimeOffset? StartedAt { get; init; } = null;
}
