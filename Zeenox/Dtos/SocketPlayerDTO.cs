using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Zeenox.Players;

namespace Zeenox.Dtos;

public class SocketPlayerDTO(PlayerState state, TrackRepeatMode repeatMode, int volume, int position, bool isAutoPlayEnabled, List<SocketUserDTO> listeners)
{
    public PlayerState State { get; } = state;
    public TrackRepeatMode TrackRepeatMode { get; } = repeatMode;
    public int Volume { get; } = volume;
    public int Position { get; } = position;
    public bool IsAutoPlayEnabled { get; } = isAutoPlayEnabled;
    public List<SocketUserDTO> Listeners { get; } = listeners;

    public SocketPlayerDTO(SocketPlayer player) : this(
        player.State,
        player.RepeatMode,
        (int)Math.Round(player.Volume * 200),
        player.Position.HasValue ? (int)player.Position.Value.Position.TotalSeconds : 0,
        player.IsAutoPlayEnabled,
        player.VoiceChannel.ConnectedUsers
            .Where(x => !x.IsBot)
            .Select(u => new SocketUserDTO(u))
            .ToList())
    {
    }
}
