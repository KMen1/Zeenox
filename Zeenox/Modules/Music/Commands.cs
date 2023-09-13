using System.Text;
using Discord;
using Discord.Interactions;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Lavalink4NET.Players;
using Lavalink4NET.Rest.Entities.Tracks;
using Zeenox.Models;
using Zeenox.Modules.Music.Preconditions;

namespace Zeenox.Modules.Music;

[RequireContext(ContextType.Guild)]
public class Commands : MusicBase
{
    public InteractiveService InteractiveService { get; set; } = null!;

    [RequireWhitelistedChannel]
    [RequireWhitelistedRole]
    [SlashCommand("play", "Plays a song. The bot will join the channel you are currently in.")]
    public async Task PlayAsync(
        [
            Summary(
                "query",
                "URL or title of song. (Supported: Spotify, Youtube, SoundCloud, BandCamp)"
            ),
            Autocomplete(typeof(SearchAutocompleteHandler))
        ]
            string query
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

        var tracks = results.Tracks.ToList();

        var index = results.Playlist is not null
            ? await player
                .PlayAsync(
                    tracks.Select(x => new ZeenoxTrackItem(new TrackReference(x), Context.User))
                )
                .ConfigureAwait(false)
            : await player
                .PlayAsync(new ZeenoxTrackItem(new TrackReference(tracks[0]), Context.User))
                .ConfigureAwait(false);

        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);

        if (index != 0)
        {
            await MusicService
                .UpdateSocketsAsync(Context.Guild.Id, updateQueue: true)
                .ConfigureAwait(false);
        }
    }

    [RequireWhitelistedChannel]
    [RequireWhitelistedRole]
    [SlashCommand(
        "play-fav",
        "Plays all of your favorited songs. The bot will join the channel you are currently in."
    )]
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

        var tracks = favorites.Select(
            x => new ZeenoxTrackItem(new TrackReference(x), Context.User)
        );
        await player.PlayAsync(tracks).ConfigureAwait(false);
        await FollowupAsync("✅").ConfigureAwait(false);
    }

    [RequireWhitelistedChannel]
    [RequireWhitelistedRole]
    [SlashCommand("skipto", "Skips to a song in the queue.")]
    public async Task SkipToAsync(
        [Summary("index", "Index of song in the queue."), MinValue(1)] int index
    )
    {
        await DeferAsync(true).ConfigureAwait(false);

        var player = await TryGetPlayerAsync().ConfigureAwait(false);
        if (player is null)
            return;

        if (index > player.Queue.Count)
        {
            await FollowupAsync("Index out of range", ephemeral: true).ConfigureAwait(false);
            return;
        }

        await player.SkipToAsync(index - 1).ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
        await MusicService
            .UpdateSocketsAsync(Context.Guild.Id, updateQueue: true)
            .ConfigureAwait(false);
    }

    [RequireWhitelistedChannel]
    [RequireWhitelistedRole]
    [SlashCommand("remove", "Removes a song from the queue.")]
    public async Task RemoveAsync(
        [Summary("index", "Index of song in the queue."), MinValue(1)] int index
    )
    {
        await DeferAsync(true).ConfigureAwait(false);

        var player = await TryGetPlayerAsync().ConfigureAwait(false);
        if (player is null)
            return;

        if (index > player.Queue.Count)
        {
            await FollowupAsync("Index out of range", ephemeral: true).ConfigureAwait(false);
            return;
        }

        await player.RemoveAsync(index - 1).ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
        await MusicService
            .UpdateSocketsAsync(Context.Guild.Id, updateQueue: true)
            .ConfigureAwait(false);
    }

    [RequireWhitelistedChannel]
    [RequireWhitelistedRole]
    [SlashCommand("volume", "Sets the volume.")]
    public async Task VolumeAsync(
        [MinValue(1), MaxValue(100), Summary("volume", "Volume between 1 and 100.")] int volume
    )
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

    [RequireWhitelistedChannel]
    [RequireWhitelistedRole]
    [SlashCommand("shuffle", "Shuffles the queue.")]
    public async Task ShuffleAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var player = await TryGetPlayerAsync().ConfigureAwait(false);
        if (player is null)
            return;

        await player.ShuffleAsync().ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
        await MusicService
            .UpdateSocketsAsync(Context.Guild.Id, updateQueue: true)
            .ConfigureAwait(false);
    }

    [RequireWhitelistedChannel]
    [RequireWhitelistedRole]
    [SlashCommand("distinct", "Removes duplicates from the queue.")]
    public async Task DistinctAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var player = await TryGetPlayerAsync().ConfigureAwait(false);
        if (player is null)
            return;

        await player.DistinctQueueAsync().ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
        await MusicService
            .UpdateSocketsAsync(Context.Guild.Id, updateQueue: true)
            .ConfigureAwait(false);
    }

    [RequireWhitelistedChannel]
    [SlashCommand("queue", "Shows the queue.")]
    public async Task ShowQueueAsync()
    {
        var player = await TryGetPlayerAsync().ConfigureAwait(false);
        if (player is null)
            return;

        var queue = player.Queue;
        if (queue.Count == 0)
        {
            await FollowupAsync("Queue is empty", ephemeral: true).ConfigureAwait(false);
            return;
        }

        var builder = new StringBuilder();
        var index = 0;
        var pages = queue
            .Select(x => x.GetTitle())
            .Chunk(10)
            .Select(x =>
            {
                for (var i = 0; i < x.Length; i++)
                {
                    builder.AppendLine($"`{i + 1 + index}. {x[i]}`");
                }
                index += x.Length;
                var pageBuilder = new PageBuilder()
                    .WithTitle("Current Queue")
                    .WithDescription(builder.ToString());
                builder.Clear();
                return pageBuilder;
            });

        var paginator = new StaticPaginatorBuilder().WithPages(pages).Build();

        await InteractiveService
            .SendPaginatorAsync(
                paginator,
                Context.Interaction,
                ephemeral: true,
                timeout: TimeSpan.FromMinutes(5)
            )
            .ConfigureAwait(false);
    }

    [RequireWhitelistedChannel]
    [RequireWhitelistedRole]
    [SlashCommand("clear", "Clears the queue.")]
    public async Task ClearAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var player = await TryGetPlayerAsync().ConfigureAwait(false);
        if (player is null)
            return;

        await player.ClearQueueAsync().ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
        await MusicService
            .UpdateSocketsAsync(Context.Guild.Id, updateQueue: true)
            .ConfigureAwait(false);
    }

    [RequireWhitelistedChannel]
    [RequireWhitelistedRole]
    [SlashCommand("reverse", "Reverses the order of the queue.")]
    public async Task ReverseAsync()
    {
        await DeferAsync(true).ConfigureAwait(false);
        var player = await TryGetPlayerAsync().ConfigureAwait(false);
        if (player is null)
            return;

        await player.ReverseQueueAsync().ConfigureAwait(false);
        await FollowupAsync("✅", ephemeral: true).ConfigureAwait(false);
        await MusicService
            .UpdateSocketsAsync(Context.Guild.Id, updateQueue: true)
            .ConfigureAwait(false);
    }

    [RequireWhitelistedChannel]
    [RequireWhitelistedRole]
    [SlashCommand("lyrics", "Shows the lyrics of the current song.")]
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

    [RequireUserPermission(GuildPermission.ManageGuild)]
    [SlashCommand("whitelist-role", "Whitelists a role or removes it from the whitelist.")]
    public async Task WhitelistRoleAsync(IRole role)
    {
        await DeferAsync(true).ConfigureAwait(false);

        var removed = false;
        await DatabaseService
            .UpdateGuildConfigAsync(
                Context.Guild.Id,
                x =>
                {
                    var allowed = x.MusicSettings.WhiteListRoles;
                    if (allowed.Contains(role.Id))
                    {
                        allowed.Remove(role.Id);
                        removed = true;
                    }
                    else
                        allowed.Add(role.Id);
                }
            )
            .ConfigureAwait(false);

        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithTitle(removed ? "Removed role from whitelist" : "Added role to whitelist")
                    .Build(),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }

    [RequireUserPermission(GuildPermission.ManageGuild)]
    [SlashCommand("whitelist-channel", "Whitelists a channel or removes it from the whitelist.")]
    public async Task WhitelistChannelAsync(IVoiceChannel channel)
    {
        await DeferAsync(true).ConfigureAwait(false);

        var removed = false;
        await DatabaseService
            .UpdateGuildConfigAsync(
                Context.Guild.Id,
                x =>
                {
                    var allowed = x.MusicSettings.WhitelistChannels;
                    if (allowed.Contains(channel.Id))
                    {
                        allowed.Remove(channel.Id);
                        removed = true;
                    }
                    else
                        allowed.Add(channel.Id);
                }
            )
            .ConfigureAwait(false);

        await FollowupAsync(
                embed: new EmbedBuilder()
                    .WithTitle(
                        removed ? "Removed channel from whitelist" : "Added channel to whitelist"
                    )
                    .Build(),
                ephemeral: true
            )
            .ConfigureAwait(false);
    }
}
