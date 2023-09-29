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
        int position,
        List<DiscordUserData> listeners
    )
    {
        ShouldUpdate = shouldUpdate;
        State = state;
        RepeatMode = repeatMode;
        Volume = volume;
        Position = position;
        Listeners = listeners;
    }

    public bool ShouldUpdate { get; init; }
    public PlayerState State { get; init; }
    public TrackRepeatMode RepeatMode { get; init; }
    public int Volume { get; init; }
    public int Position { get; init; }
    public List<DiscordUserData> Listeners { get; init; }

    public static PlayerData FromZeenoxPlayer(ZeenoxPlayer player)
    {
        return new PlayerData(
            true,
            player.State,
            player.RepeatMode,
            (int)Math.Round(player.Volume * 200),
            player.Position.HasValue ? (int)player.Position.Value.Position.TotalSeconds : 0,
            player.VoiceChannel.ConnectedUsers.Where(x => !x.IsBot).Select(DiscordUserData.FromUser).ToList()
        );
    }

    public static PlayerData Empty =>
        new(false, PlayerState.NotPlaying, TrackRepeatMode.None, 0, 0, Array.Empty<DiscordUserData>().ToList());
}
