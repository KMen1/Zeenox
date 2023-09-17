using Discord.Interactions;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Zeenox.Modules.Music.Preconditions;

namespace Zeenox.Modules.Music;

[RateLimit(5, 2)]
[RequireWhitelistedRole]
[RequireContext(ContextType.Guild)]
public class Interactions : MusicBase
{
    [ComponentInteraction("volumeup")]
    public async Task VolumeUpAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var player = await TryGetPlayerAsync().ConfigureAwait(false);
        if (player is null)
            return;

        var volume = (int)(player.Volume * 100);
        volume += 10;
        await player.SetVolumeAsync(volume / 100f).ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("volumedown")]
    public async Task VolumeDownAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var player = await TryGetPlayerAsync().ConfigureAwait(false);
        if (player is null)
            return;

        var volume = (int)(player.Volume * 100);
        volume -= 10;
        await player.SetVolumeAsync(volume / 100f).ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("pause")]
    public async Task PauseAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var player = await TryGetPlayerAsync().ConfigureAwait(false);
        if (player is null)
            return;

        if (player.State is PlayerState.Paused)
        {
            await player.ResumeAsync().ConfigureAwait(false);
        }
        else
        {
            await player.PauseAsync().ConfigureAwait(false);
        }
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
        await MusicService.UpdateSocketsAsync(Context.Guild.Id, true).ConfigureAwait(false);
    }

    [ComponentInteraction("skip")]
    public async Task SkipAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var player = await TryGetPlayerAsync().ConfigureAwait(false);
        if (player is null)
            return;

        await player.SkipAsync().ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("previous")]
    public async Task PreviousAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var player = await TryGetPlayerAsync().ConfigureAwait(false);
        if (player is null)
            return;

        await player.RewindAsync().ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("loop")]
    public async Task LoopAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var player = await TryGetPlayerAsync().ConfigureAwait(false);
        if (player is null)
            return;

        var shouldDisable = !Enum.IsDefined(typeof(TrackRepeatMode), player.RepeatMode + 1);
        await player
            .SetLoopModeAsync(shouldDisable ? 0 : player.RepeatMode + 1)
            .ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
        await MusicService.UpdateSocketsAsync(Context.Guild.Id, true).ConfigureAwait(false);
    }

    [ComponentInteraction("stop")]
    public async Task StopAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var player = await TryGetPlayerAsync().ConfigureAwait(false);
        if (player is null)
            return;

        await player.StopAsync().ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("disconnect")]
    public async Task DisconnectAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var player = await TryGetPlayerAsync().ConfigureAwait(false);
        if (player is null)
            return;

        await player.DeleteNowPlayingMessageAsync().ConfigureAwait(false);
        await player.DisconnectAsync().ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("favorite")]
    public async Task FavoriteAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var player = await TryGetPlayerAsync().ConfigureAwait(false);
        if (player?.CurrentTrack is null)
        {
            await FollowupAsync("There is no song playing right now.").ConfigureAwait(false);
            return;
        }

        var trackString = player.CurrentTrack.ToString();
        await DatabaseService
            .UpdateUserAsync(
                Context.User.Id,
                x =>
                {
                    if (x.FavoriteSongs.Contains(trackString))
                        x.FavoriteSongs.Remove(trackString);
                    else
                        x.FavoriteSongs.Add(trackString);
                }
            )
            .ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }
}
