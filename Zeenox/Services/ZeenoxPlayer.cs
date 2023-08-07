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
    }

    public override async ValueTask PlayAsync(
        TrackReference trackReference,
        TrackPlayProperties properties = new(),
        CancellationToken cancellationToken = new()
    )
    {
        await base.PlayAsync(trackReference, properties, cancellationToken).ConfigureAwait(false);
        if (NowPlayingMessage is not null)
            await UpdateNowPlayingMessageAsync().ConfigureAwait(false);
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
        await UpdateNowPlayingMessageAsync().ConfigureAwait(false);
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
        await UpdateNowPlayingMessageAsync().ConfigureAwait(false);
    }

    public override async ValueTask ResumeAsync(CancellationToken cancellationToken = new())
    {
        await base.ResumeAsync(cancellationToken).ConfigureAwait(false);
        await UpdateNowPlayingMessageAsync().ConfigureAwait(false);
    }

    public Task SetLoopModeAsync(TrackRepeatMode repeatMode)
    {
        RepeatMode = repeatMode;
        return UpdateNowPlayingMessageAsync();
    }

    public async Task ClearQueueAsync()
    {
        await Queue.ClearAsync().ConfigureAwait(false);
        await UpdateNowPlayingMessageAsync().ConfigureAwait(false);
    }

    public async Task DistinctQueueAsync()
    {
        await Queue.DistinctAsync().ConfigureAwait(false);
        await UpdateNowPlayingMessageAsync().ConfigureAwait(false);
    }

    public async Task ReverseQueueAsync()
    {
        var reversed = Queue.Reverse().ToList();
        await Queue.ClearAsync().ConfigureAwait(false);
        await Queue.AddRangeAsync(reversed).ConfigureAwait(false);
        await UpdateNowPlayingMessageAsync().ConfigureAwait(false);
    }

    public override async ValueTask SetVolumeAsync(
        float volume,
        CancellationToken cancellationToken = new()
    )
    {
        await base.SetVolumeAsync(volume, cancellationToken).ConfigureAwait(false);
        await UpdateNowPlayingMessageAsync().ConfigureAwait(false);
    }

    private async Task UpdateNowPlayingMessageAsync(LavalinkTrack? track = null)
    {
        var coverUrl = "";
        if (track is null && CurrentTrack is not null)
        {
            if (CurrentTrack.SourceName == "spotify")
            {
                coverUrl = await SpotifyService
                    .GetCoverUrl(CurrentTrack.Identifier)
                    .ConfigureAwait(false);
            }
            else
            {
                coverUrl = (
                    await ArtworkService.ResolveAsync(CurrentTrack).ConfigureAwait(false)
                )?.ToString();
            }
        }

        if (track is not null)
        {
            if (track.SourceName == "spotify")
            {
                coverUrl = await SpotifyService.GetCoverUrl(track.Identifier).ConfigureAwait(false);
            }
            else
            {
                coverUrl = (
                    await ArtworkService.ResolveAsync(track).ConfigureAwait(false)
                )?.ToString();
            }
        }

        var eb = track is not null
            ? new NowPlayingEmbed(track, Volume, Queue, coverUrl)
            : CurrentTrack is not null
                ? new NowPlayingEmbed(CurrentTrack, Volume, Queue, coverUrl)
                : new EmbedBuilder().WithTitle(
                    "No song is currently playing, will disconnect in 3 minutes."
                );

        var cb = track is not null
            ? new NowPlayingButtons(
                Queue,
                State is PlayerState.Paused,
                LastVoteSkipInfo,
                Volume,
                RepeatMode
            )
            : CurrentTrack is not null
                ? new NowPlayingButtons(
                    Queue,
                    State is PlayerState.Paused,
                    LastVoteSkipInfo,
                    Volume,
                    RepeatMode
                )
                : new ComponentBuilder();

        if (NowPlayingMessage is null)
        {
            NowPlayingMessage = await TextChannel
                .SendMessageAsync(embed: eb.Build(), components: cb.Build())
                .ConfigureAwait(false);
        }
        else
        {
            var check = await TextChannel
                .GetMessageAsync(NowPlayingMessage.Id)
                .ConfigureAwait(false);
            if (check is null)
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
    }

    protected override async ValueTask OnTrackStartedAsync(
        LavalinkTrack track,
        CancellationToken cancellationToken = new()
    )
    {
        await base.OnTrackStartedAsync(track, cancellationToken).ConfigureAwait(false);
        await UpdateNowPlayingMessageAsync(track).ConfigureAwait(false);
    }

    public override async ValueTask StopAsync(
        bool disconnect = false,
        CancellationToken cancellationToken = new CancellationToken()
    )
    {
        await base.StopAsync(disconnect, cancellationToken).ConfigureAwait(false);
        await UpdateNowPlayingMessageAsync().ConfigureAwait(false);
    }

    public Task DeleteMessageAsync()
    {
        return NowPlayingMessage is not null ? NowPlayingMessage.DeleteAsync() : Task.CompletedTask;
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
