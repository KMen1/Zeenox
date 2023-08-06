using Discord;
using Discord.Interactions;
using Zeenox.Modules.Music.Preconditions;

namespace Zeenox.Modules.Music;

[RequireContext(ContextType.Guild)]
public class Commands : MusicBase
{
    [RequireVoiceChannel]
    [SlashCommand("play", "Plays a song")]
    public async Task PlayAsync(string query)
    {
        await DeferAsync(true);
        await MusicService.PlayAsync(
            Context.Guild.Id,
            (ITextChannel)Context.Channel,
            VoiceState!.VoiceChannel,
            Context.User,
            query
        );
        await FollowupAsync("✅", ephemeral: true);
    }

    [RequireVoiceChannel]
    [SlashCommand("play-fav", "Plays your favorites songs")]
    public async Task PlayFavAsync()
    {
        await DeferAsync(true);
        /*var favorites = (await DatabaseService.GetUserAsync(Context.User.Id)).FavoriteSongs;
        if (favorites.Count == 0)
        {
            await FollowupAsync("You don't have any favorite songs", ephemeral: true);
            return;
        }

        var tracks = favorites.Select(TrackDecoder.DecodeTrack);
        await MusicService.PlayAsync(
            Context.Guild.Id,
            (ITextChannel)Context.Channel,
            VoiceState!.VoiceChannel,
            Context.User,
            tracks
        );*/
        await FollowupAsync("✅");
    }

    [RequireVoiceChannel]
    [RequirePlayer]
    [RequireSameVoiceChannel]
    [SlashCommand("volume", "Sets the volume")]
    public async Task VolumeAsync([MinValue(0), MaxValue(100)] int volume)
    {
        await DeferAsync(true);
        await MusicService.SetVolumeAsync(Context.Guild.Id, volume);
        await FollowupAsync("✅", ephemeral: true);
    }

    [RequireVoiceChannel]
    [RequirePlayer]
    [RequireSameVoiceChannel]
    [SlashCommand("shuffle", "Shuffles the queue")]
    public async Task ShuffleAsync()
    {
        await DeferAsync(true);
        await MusicService.ShuffleQueueAsync(Context.Guild.Id);
        await FollowupAsync("✅", ephemeral: true);
    }

    [RequireVoiceChannel]
    [RequirePlayer]
    [RequireSameVoiceChannel]
    [SlashCommand("distinct", "Removes duplicates from the queue")]
    public async Task DistinctAsync()
    {
        await DeferAsync(true);
        await MusicService.DistinctQueueAsync(Context.Guild.Id);
        await FollowupAsync("✅", ephemeral: true);
    }

    [RequireVoiceChannel]
    [RequirePlayer]
    [RequireSameVoiceChannel]
    [SlashCommand("clear", "Clears the queue")]
    public async Task ClearAsync()
    {
        await DeferAsync(true);
        await MusicService.ClearQueueAsync(Context.Guild.Id);
        await FollowupAsync("✅", ephemeral: true);
    }
    
    [RequireVoiceChannel]
    [RequirePlayer]
    [RequireSameVoiceChannel]
    [SlashCommand("reverse", "Reverses the queue")]
    public async Task ReverseAsync()
    {
        await DeferAsync(true);
        await MusicService.ReverseQueueAsync(Context.Guild.Id);
        await FollowupAsync("✅", ephemeral: true);
    }

    [RequireVoiceChannel]
    [RequirePlayer]
    [RequireSameVoiceChannel]
    [SlashCommand("lyrics", "Shows the lyrics of the current song")]
    public async Task LyricsAsync()
    {
        await DeferAsync(true);
        var lyrics = await MusicService.GetLyricsAsync(Context.Guild.Id);
        if (lyrics is null)
        {
            await FollowupAsync("No lyrics found", ephemeral: true);
            return;
        }
        
        await FollowupAsync(lyrics, ephemeral: true);
    }
}
