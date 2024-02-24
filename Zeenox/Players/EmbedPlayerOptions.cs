using Discord;
using Discord.WebSocket;
using Lavalink4NET.Players.Vote;

namespace Zeenox.Players;

public record EmbedPlayerOptions : MusicPlayerOptions
{
    public ITextChannel? TextChannel { get; set; }
    public SocketVoiceChannel VoiceChannel { get; set; } = null!;
}
