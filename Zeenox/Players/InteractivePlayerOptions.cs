using Discord;
using Discord.WebSocket;
using Lavalink4NET.Players.Vote;
using Zeenox.Services;

namespace Zeenox.Players;

public record InteractivePlayerOptions : VoteLavalinkPlayerOptions
{
    public ITextChannel? TextChannel { get; set; } = null!;
    public SocketVoiceChannel VoiceChannel { get; set; } = null!;
    public DatabaseService DbService { get; set; } = null!;
}
