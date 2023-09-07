using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Zeenox.Services;

namespace Zeenox.Models;

public class PlayerData
{
    private PlayerData(
        bool shouldUpdate,
        PlayerState state,
        TrackRepeatMode repeatMode,
        int volume,
        int? position
    )
    {
        ShouldUpdate = shouldUpdate;
        State = state;
        RepeatMode = repeatMode;
        Volume = volume;
        Position = position;
    }

    public bool ShouldUpdate { get; init; }
    public PlayerState State { get; init; }
    public TrackRepeatMode RepeatMode { get; init; }
    public int Volume { get; init; }
    public int? Position { get; init; }

    public static PlayerData FromZeenoxPlayer(ZeenoxPlayer player)
    {
        return new PlayerData(
            true,
            player.State,
            player.RepeatMode,
            (int)Math.Round(player.Volume * 200),
            player.Position.HasValue ? (int)player.Position.Value.Position.TotalSeconds : null
        );
    }

    public static PlayerData Empty =>
        new(false, PlayerState.NotPlaying, TrackRepeatMode.None, 0, null);
}
