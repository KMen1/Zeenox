using Discord.Interactions;
using Zeenox.Modules.Music.Preconditions;

namespace Zeenox.Modules.Music;

[RateLimit]
[RequireContext(ContextType.Guild)]
[RequirePlayer]
[RequireSameVoiceChannel]
public class Interactions : MusicBase
{
    [ComponentInteraction("volumeup")]
    public async Task VolumeUpAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        await MusicService.OffsetVolumeAsync(Context.Guild.Id, 10).ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("volumedown")]
    public async Task VolumeDownAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        await MusicService.OffsetVolumeAsync(Context.Guild.Id, -10).ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("pause")]
    public async Task PauseAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        await MusicService.PauseOrResumeAsync(Context.Guild.Id).ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("skip")]
    public async Task SkipAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        await MusicService.SkipAsync(Context.Guild.Id).ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("previous")]
    public async Task PreviousAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        await MusicService.RewindAsync(Context.Guild.Id).ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("loop")]
    public async Task LoopAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        await MusicService.CycleLoopMode(Context.Guild.Id).ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("stop")]
    public async Task StopAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        await MusicService.StopAsync(Context.Guild.Id).ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }
    
    [ComponentInteraction("favorite")]
    public async Task FavoriteAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var (playerExists, player) = await MusicService.TryGetPlayer(Context.Guild.Id).ConfigureAwait(false);
        if (!playerExists || player?.CurrentTrack is null)
        {
            await FollowupAsync("There is no song playing right now.").ConfigureAwait(false);
            return;
        }

        var trackString = player.CurrentTrack.ToString();
        await DatabaseService.UpdateUserAsync(
            Context.User.Id,
            x =>
            {
                if (x.FavoriteSongs.Contains(trackString))
                    x.FavoriteSongs.Remove(trackString);
                else
                    x.FavoriteSongs.Add(trackString);
            }
        ).ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }
}
