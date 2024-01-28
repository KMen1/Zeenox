using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Zeenox.Enums;
using Zeenox.Players;

namespace Zeenox.Models.Socket;

public class UpdatePlayerPayload(
    PlayerState state,
    TrackRepeatMode repeatMode,
    int volume,
    int position,
    List<BasicDiscordUser> listeners
) : IPayload
{
    public PayloadType Type { get; } = PayloadType.UpdatePlayer;
    public PlayerState State { get; } = state;
    public TrackRepeatMode RepeatMode { get; } = repeatMode;
    public int Volume { get; } = volume;
    public int Position { get; } = position;
    public List<BasicDiscordUser> Listeners { get; } = listeners;

    public UpdatePlayerPayload(InteractivePlayer player) : this(
        player.State,
        player.RepeatMode,
        (int)Math.Round(player.Volume * 200),
        player.Position.HasValue ? (int)player.Position.Value.Position.TotalSeconds : 0,
        player.VoiceChannel.ConnectedUsers
            .Where(x => !x.IsBot)
            .Select(u => new BasicDiscordUser(u))
            .ToList()
    )
    {
    }
}