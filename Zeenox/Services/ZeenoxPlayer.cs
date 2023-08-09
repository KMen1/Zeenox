using Discord;
using Lavalink4NET.Artwork;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Players.Vote;
using Lavalink4NET.Tracks;
using Newtonsoft.Json;
using Zeenox.Models;

namespace Zeenox.Services;

public sealed class ZeenoxPlayer : VoteLavalinkPlayer
{
    public ZeenoxPlayer(IPlayerProperties<ZeenoxPlayer, ZeenoxPlayerOptions> properties)
        : base(properties)
    {
        TextChannel = properties.Options.Value.TextChannel;
        VoiceChannel = properties.Options.Value.VoiceChannel;
        SpotifyService = properties.Options.Value.SpotifyService;
        ArtworkService = properties.Options.Value.ArtworkService;
    }

    private SpotifyService SpotifyService { get; }
    private IArtworkService ArtworkService { get; }
    private ITextChannel TextChannel { get; }
    private IVoiceChannel VoiceChannel { get; }
    private IUserMessage? NowPlayingMessage { get; set; }
    private UserVoteSkipInfo? LastVoteSkipInfo { get; set; }

    public async Task PlayAsync(IEnumerable<LavalinkTrack> tracksEnumerable)
    {
        var tracks = tracksEnumerable.ToArray();
        if (tracks.Length == 0)
            return;

        await PlayAsync(tracks[0]).ConfigureAwait(false);
        await Queue
            .AddRangeAsync(
                tracks.Select(x => new TrackQueueItem(new TrackReference(x))).Skip(1).ToList()
            )
            .ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public override async ValueTask PlayAsync(
        TrackReference trackReference,
        TrackPlayProperties properties = new(),
        CancellationToken cancellationToken = new()
    )
    {
        await base.PlayAsync(trackReference, properties, cancellationToken).ConfigureAwait(false);
        if (NowPlayingMessage is not null)
            await UpdateMessageAsync().ConfigureAwait(false);
    }

    public async Task RewindAsync()
    {
        if (!Queue.HasHistory)
            return;

        var track = Queue.History[^1];
        await Queue.History.RemoveAtAsync(Queue.History.Count - 1).ConfigureAwait(false);

        await PlayAsync(track, false).ConfigureAwait(false);
    }

    public override async ValueTask<UserVoteSkipInfo> VoteAsync(
        ulong userId,
        float percentage = 0.5f
    )
    {
        var result = await base.VoteAsync(userId, percentage).ConfigureAwait(false);
        LastVoteSkipInfo = result;
        await UpdateMessageAsync().ConfigureAwait(false);
        return result;
    }

    public override void ClearVotes()
    {
        LastVoteSkipInfo = null;
        base.ClearVotes();
    }

    public override async ValueTask PauseAsync(CancellationToken cancellationToken = new())
    {
        await base.PauseAsync(cancellationToken).ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public override async ValueTask ResumeAsync(CancellationToken cancellationToken = new())
    {
        await base.ResumeAsync(cancellationToken).ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public Task SetLoopModeAsync(TrackRepeatMode repeatMode)
    {
        RepeatMode = repeatMode;
        return UpdateMessageAsync();
    }

    public async Task ClearQueueAsync()
    {
        await Queue.ClearAsync().ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public async Task DistinctQueueAsync()
    {
        await Queue.DistinctAsync().ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public async Task ReverseQueueAsync()
    {
        var reversed = Queue.Reverse().ToList();
        await Queue.ClearAsync().ConfigureAwait(false);
        await Queue.AddRangeAsync(reversed).ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public override async ValueTask SetVolumeAsync(
        float volume,
        CancellationToken cancellationToken = new()
    )
    {
        await base.SetVolumeAsync(volume, cancellationToken).ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    private async Task UpdateMessageAsync(LavalinkTrack? track = null)
    {
        var eb = await GetEmbedBuilder(track ?? CurrentTrack).ConfigureAwait(false);
        var cb = GetButtons(track ?? CurrentTrack);

        if (
            NowPlayingMessage is null
            || await TextChannel.GetMessageAsync(NowPlayingMessage.Id).ConfigureAwait(false) is null
        )
        {
            NowPlayingMessage = await TextChannel
                .SendMessageAsync(embed: eb.Build(), components: cb.Build())
                .ConfigureAwait(false);
        }
        else
        {
            await NowPlayingMessage
                .ModifyAsync(x =>
                {
                    x.Embed = eb.Build();
                    x.Components = cb.Build();
                })
                .ConfigureAwait(false);
        }
    }

    private async Task<string?> GetCoverUrl(LavalinkTrack? track)
    {
        if (track is null)
            return null;

        string? coverUrl;
        if (track.SourceName == "spotify")
        {
            coverUrl = await SpotifyService.GetCoverUrl(track.Identifier).ConfigureAwait(false);
        }
        else
        {
            coverUrl = (await ArtworkService.ResolveAsync(track).ConfigureAwait(false))?.ToString();
        }

        return coverUrl;
    }

    private async Task<EmbedBuilder> GetEmbedBuilder(LavalinkTrack? track)
    {
        if (track is null)
        {
            return new EmbedBuilder().WithTitle(
                "No song is currently playing, will disconnect in 3 minutes."
            );
        }

        var coverUrl = await GetCoverUrl(track).ConfigureAwait(false);
        return new NowPlayingEmbed(track, Volume, Queue, coverUrl);
    }

    private ComponentBuilder GetButtons(LavalinkTrack? track)
    {
        if (track is null)
            return new ComponentBuilder();

        return new NowPlayingButtons(
            Queue,
            State is PlayerState.Paused,
            LastVoteSkipInfo,
            Volume,
            RepeatMode
        );
    }

    protected override async ValueTask OnTrackStartedAsync(
        LavalinkTrack track,
        CancellationToken cancellationToken = new()
    )
    {
        await base.OnTrackStartedAsync(track, cancellationToken).ConfigureAwait(false);
        await UpdateMessageAsync(track).ConfigureAwait(false);
    }

    public override async ValueTask StopAsync(
        bool disconnect = false,
        CancellationToken cancellationToken = new()
    )
    {
        await base.StopAsync(disconnect, cancellationToken).ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public Task DeleteMessageAsync()
    {
        return NowPlayingMessage?.DeleteAsync() ?? Task.CompletedTask;
    }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(
            new
            {
                TextChannelId = TextChannel.Id,
                VoiceChannelId = VoiceChannel.Id,
                TextChannelName = TextChannel.Name,
                VoiceChannelName = VoiceChannel.Name,
                IsPlaying = State is PlayerState.Playing,
                IsPaused = State is PlayerState.Paused,
                Queue,
                Volume,
                RepeatMode
            }
        );
    }
}
