using System.Collections.Immutable;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Zeenox.Players;

namespace Zeenox.Dtos;

public class SocketPlayerDTO(
    PlayerState state,
    TrackRepeatMode repeatMode,
    int volume,
    double position,
    bool isAutoPlayEnabled,
    ImmutableArray<SocketUserDTO> listeners)
{
    public SocketPlayerDTO(SocketPlayer player) : this(
        player.State,
        player.RepeatMode,
        (int)Math.Round(player.Volume * 200),
        player.Position.HasValue ? player.Position.Value.Position.TotalMilliseconds : 0,
        player.IsAutoPlayEnabled,
        player.VoiceChannel.ConnectedUsers
              .Where(x => !x.IsBot)
              .Select(u => new SocketUserDTO(u))
              .ToImmutableArray()) { }

    public PlayerState State { get; } = state;
    public TrackRepeatMode TrackRepeatMode { get; } = repeatMode;
    public int Volume { get; } = volume;
    public double Position { get; } = position;
    public bool IsAutoPlayEnabled { get; } = isAutoPlayEnabled;
    public ImmutableArray<SocketUserDTO> Listeners { get; } = listeners;
}