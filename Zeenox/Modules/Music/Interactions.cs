using Discord.Interactions;

namespace Zeenox.Modules.Music;

public class Interactions : MusicBase
{
    [ComponentInteraction("volumeup")]
    public async Task VolumeUpAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        await MusicService.SetVolumeAsync(Context.Guild.Id, x => x += 10).ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [ComponentInteraction("volumedown")]
    public async Task VolumeDownAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        await MusicService.SetVolumeAsync(Context.Guild.Id, x => x -= 10).ConfigureAwait(false);
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
}
