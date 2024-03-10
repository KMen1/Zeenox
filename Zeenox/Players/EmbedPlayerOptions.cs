using Discord;
using Discord.WebSocket;

namespace Zeenox.Players;

public record EmbedPlayerOptions : MusicPlayerOptions
{
    public ITextChannel? TextChannel { get; set; }
    public SocketVoiceChannel VoiceChannel { get; set; } = null!;
}