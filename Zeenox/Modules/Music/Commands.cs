using Discord;
using Discord.Interactions;
using Lavalink4NET.Tracks;
using Zeenox.Modules.Music.Preconditions;

namespace Zeenox.Modules.Music;

[RequireContext(ContextType.Guild)]
public class Commands : MusicBase
{
    [RequireVoiceChannel]
    [SlashCommand("play", "Plays a song")]
    public async Task PlayAsync(string query)
    {
        await DeferAsync(true).ConfigureAwait(false);
        await MusicService
            .PlayAsync(
                Context.Guild.Id,
                (ITextChannel)Context.Channel,
                VoiceState!.VoiceChannel,
                Context.User,
                query
            )
            .ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [RequireVoiceChannel]
    [SlashCommand("play-fav", "Plays your favorites songs")]
    public async Task PlayFavAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var favorites = (
            await DatabaseService.GetUserAsync(Context.User.Id).ConfigureAwait(false)
        ).FavoriteSongs;
        if (favorites.Count == 0)
        {
            await FollowupAsync("You don't have any favorite songs", ephemeral: true)
                .ConfigureAwait(false);
            return;
        }

        var tracks = favorites.Select(x => LavalinkTrack.Parse(x, null));
        await MusicService
            .PlayAsync(
                Context.Guild.Id,
                (ITextChannel)Context.Channel,
                VoiceState!.VoiceChannel,
                Context.User,
                tracks
            )
            .ConfigureAwait(false);
        await FollowupAsync("✅").ConfigureAwait(false);
    }

    [RequireVoiceChannel]
    [RequirePlayer]
    [RequireSameVoiceChannel]
    [SlashCommand("volume", "Sets the volume")]
    public async Task VolumeAsync([MinValue(0), MaxValue(100)] int volume)
    {
        await DeferAsync(true).ConfigureAwait(false);
        await MusicService.SetVolumeAsync(Context.Guild.Id, volume).ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [RequireVoiceChannel]
    [RequirePlayer]
    [RequireSameVoiceChannel]
    [SlashCommand("shuffle", "Shuffles the queue")]
    public async Task ShuffleAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        await MusicService.ShuffleQueueAsync(Context.Guild.Id).ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [RequireVoiceChannel]
    [RequirePlayer]
    [RequireSameVoiceChannel]
    [SlashCommand("distinct", "Removes duplicates from the queue")]
    public async Task DistinctAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        await MusicService.DistinctQueueAsync(Context.Guild.Id).ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [RequireVoiceChannel]
    [RequirePlayer]
    [RequireSameVoiceChannel]
    [SlashCommand("clear", "Clears the queue")]
    public async Task ClearAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        await MusicService.ClearQueueAsync(Context.Guild.Id).ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [RequireVoiceChannel]
    [RequirePlayer]
    [RequireSameVoiceChannel]
    [SlashCommand("reverse", "Reverses the queue")]
    public async Task ReverseAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        await MusicService.ReverseQueueAsync(Context.Guild.Id).ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [RequireVoiceChannel]
    [RequirePlayer]
    [RequireSameVoiceChannel]
    [SlashCommand("lyrics", "Shows the lyrics of the current song")]
    public async Task LyricsAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var lyrics = await MusicService.GetLyricsAsync(Context.Guild.Id).ConfigureAwait(false);
        if (lyrics is null)
        {
            await FollowupAsync("No lyrics found", ephemeral: true).ConfigureAwait(false);
            return;
        }

        await FollowupAsync(lyrics, ephemeral: true).ConfigureAwait(false);
    }
}
