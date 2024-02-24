using Discord.WebSocket;
using Zeenox.Models;
using Zeenox.Services;

namespace Zeenox.Players;

public record SocketPlayerOptions : EmbedPlayerOptions
{
    public DiscordSocketClient DiscordClient { get; set; } = null!;
    public DatabaseService DbService { get; set; } = null!;
    public ResumeSession? ResumeSession { get; set; }
}
