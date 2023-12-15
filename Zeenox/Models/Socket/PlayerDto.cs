using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Zeenox.Players;

namespace Zeenox.Models.Socket;

public class PlayerDto(
    PlayerState state,
    TrackRepeatMode repeatMode,
    int volume,
    int position,
    List<UserDto> listeners
) : ISocketMessageData
{
    public string Type { get; } = "player-data";
    public PlayerState State { get; } = state;
    public TrackRepeatMode RepeatMode { get; } = repeatMode;
    public int Volume { get; } = volume;
    public int Position { get; } = position;
    public List<UserDto> Listeners { get; } = listeners;

    public PlayerDto(InteractivePlayer player) : this(
        player.State,
        player.RepeatMode,
        (int)Math.Round(player.Volume * 200),
        player.Position.HasValue ? (int)player.Position.Value.Position.TotalSeconds : 0,
        player.VoiceChannel.ConnectedUsers
            .Where(x => !x.IsBot)
            .Select(u => new UserDto(u))
            .ToList()
    )
    {
    }
}