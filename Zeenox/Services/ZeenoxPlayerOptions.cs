using Discord;
using Lavalink4NET.Players.Vote;

namespace Zeenox.Services;

public sealed record class ZeenoxPlayerOptions : VoteLavalinkPlayerOptions
{
    public ITextChannel TextChannel { get; set; } = null!;
    public IVoiceChannel VoiceChannel { get; set; } = null!;
    public SpotifyService SpotifyService { get; set; } = null!;
}
