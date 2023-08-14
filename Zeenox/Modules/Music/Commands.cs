using Discord;
using Discord.Interactions;
using Lavalink4NET.Players;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.Tracks;

namespace Zeenox.Modules.Music;

[RequireContext(ContextType.Guild)]
public class Commands : MusicBase
{
    [SlashCommand("play", "Plays a song")]
    public async Task PlayAsync(
        [Summary("query"), Autocomplete(typeof(SearchAutocompleteHandler))] string query
    )
    {
        await DeferAsync(true).ConfigureAwait(false);

        var player = await TryGetPlayerAsync(true, isDeferred: true).ConfigureAwait(false);
        if (player is null)
            return;

        var results = await AudioService.Tracks
            .LoadTracksAsync(
                query,
                new TrackLoadOptions
                {
                    SearchMode = Uri.IsWellFormedUriString(query, UriKind.Absolute)
                        ? TrackSearchMode.None
                        : TrackSearchMode.YouTube
                }
            )
            .ConfigureAwait(false);

        if (!results.HasMatches || results.Tracks.Length == 0)
        {
            await FollowupAsync(
                    embed: new EmbedBuilder()
                        .WithColor(Color.Red)
                        .WithTitle($"No matches found for: {query}")
                        .Build(),
                    ephemeral: true
                )
                .ConfigureAwait(false);
        }

        var tracks = results.Tracks;
        if (results.IsPlaylist)
        {
            await player.PlayAsync(tracks).ConfigureAwait(false);
        }
        else
        {
            await player
                .PlayAsync(new TrackReference(tracks[0]), default(TrackPlayProperties))
                .ConfigureAwait(false);
        }

        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

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

        var player = await TryGetPlayerAsync(true, isDeferred: true).ConfigureAwait(false);
        if (player is null)
            return;

        var tracks = favorites.Select(x => LavalinkTrack.Parse(x, null));
        await player.PlayAsync(tracks).ConfigureAwait(false);
        await FollowupAsync("✅").ConfigureAwait(false);
    }

    [SlashCommand("volume", "Sets the volume")]
    public async Task VolumeAsync([MinValue(0), MaxValue(100)] int volume)
    {
        await DeferAsync(true).ConfigureAwait(false);
        var player = await TryGetPlayerAsync().ConfigureAwait(false);
        if (player is null)
            return;

        await player
            .SetVolumeAsync((float)Math.Floor(volume / (double)2) / 100f)
            .ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("shuffle", "Shuffles the queue")]
    public async Task ShuffleAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var player = await TryGetPlayerAsync().ConfigureAwait(false);
        if (player is null)
            return;

        await player.Queue.ShuffleAsync().ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("distinct", "Removes duplicates from the queue")]
    public async Task DistinctAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var player = await TryGetPlayerAsync().ConfigureAwait(false);
        if (player is null)
            return;

        await player.DistinctQueueAsync().ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("clear", "Clears the queue")]
    public async Task ClearAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var player = await TryGetPlayerAsync().ConfigureAwait(false);
        if (player is null)
            return;

        await player.ClearQueueAsync().ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

    [SlashCommand("reverse", "Reverses the queue")]
    public async Task ReverseAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var player = await TryGetPlayerAsync().ConfigureAwait(false);
        if (player is null)
            return;

        await player.ReverseQueueAsync().ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
    }

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
