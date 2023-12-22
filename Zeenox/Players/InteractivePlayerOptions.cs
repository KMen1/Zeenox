using Discord;
using Discord.WebSocket;
using Lavalink4NET.Players.Vote;

namespace Zeenox.Players;

public record InteractivePlayerOptions : VoteLavalinkPlayerOptions
{
    public ITextChannel? TextChannel { get; set; } = null!;
    public SocketVoiceChannel VoiceChannel { get; set; } = null!;
}
