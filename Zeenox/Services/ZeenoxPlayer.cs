using Discord;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Players.Vote;
using Lavalink4NET.Protocol.Payloads.Events;
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
    }

    private SpotifyService SpotifyService { get; }
    private ITextChannel TextChannel { get; }
    private IVoiceChannel VoiceChannel { get; }
    private IUserMessage? NowPlayingMessage { get; set; }
    private List<LavalinkTrack> History { get; } = new();
    private UserVoteSkipInfo? LastVoteSkipInfo { get; set; }

    public async Task PlayAsync(IEnumerable<LavalinkTrack> tracksEnumerable)
    {
        var tracks = tracksEnumerable.ToArray();
        if (tracks.Length == 0)
            return;

        await PlayAsync(tracks[0]);
        await Queue.AddRangeAsync(
            tracks.Select(x => new TrackQueueItem(new TrackReference(x))).Skip(1).ToList()
        );
    }

    public override async ValueTask PlayAsync(
        TrackReference trackReference,
        TrackPlayProperties properties = new(),
        CancellationToken cancellationToken = new()
    )
    {
        await base.PlayAsync(trackReference, properties, cancellationToken);
        await UpdateNowPlayingMessageAsync();
    }

    public override async ValueTask SkipAsync(
        int count = 1,
        CancellationToken cancellationToken = new()
    )
    {
        await base.SkipAsync(count, cancellationToken);
        await UpdateNowPlayingMessageAsync();
    }

    public ValueTask<int> RewindAsync()
    {
        var track = History[^1];
        History.RemoveAt(History.Count - 1);
        return PlayAsync(track, false);
    }

    public override async ValueTask<UserVoteSkipInfo> VoteAsync(
        ulong userId,
        float percentage = 0.5f
    )
    {
        var result = await base.VoteAsync(userId, percentage);
        LastVoteSkipInfo = result;
        await UpdateNowPlayingMessageAsync();
        return result;
    }

    public override void ClearVotes()
    {
        LastVoteSkipInfo = null;
        base.ClearVotes();
    }

    public override async ValueTask PauseAsync(CancellationToken cancellationToken = new())
    {
        await base.PauseAsync(cancellationToken);
        await UpdateNowPlayingMessageAsync();
    }

    public override async ValueTask ResumeAsync(CancellationToken cancellationToken = new())
    {
        await base.ResumeAsync(cancellationToken);
        await UpdateNowPlayingMessageAsync();
    }

    public Task SetLoopModeAsync(TrackRepeatMode repeatMode)
    {
        RepeatMode = repeatMode;
        return UpdateNowPlayingMessageAsync();
    }

    public async Task ClearQueueAsync()
    {
        await Queue.ClearAsync();
        await UpdateNowPlayingMessageAsync();
    }

    public async Task DistinctQueueAsync()
    {
        await Queue.DistinctAsync();
        await UpdateNowPlayingMessageAsync();
    }
    
    public async Task ReverseQueueAsync()
    {
        var reversed = Queue.Reverse().ToList();
        await Queue.ClearAsync();
        await Queue.AddRangeAsync(reversed);
        await UpdateNowPlayingMessageAsync();
    }

    public override async ValueTask SetVolumeAsync(
        float volume,
        CancellationToken cancellationToken = new()
    )
    {
        await base.SetVolumeAsync(volume, cancellationToken);
        await UpdateNowPlayingMessageAsync();
    }

    private async Task UpdateNowPlayingMessageAsync(LavalinkTrack? track = null)
    {
        var coverUrl = track is not null
            ? await SpotifyService.GetCoverUrl(track.Identifier)
            : CurrentTrack is not null
                ? await SpotifyService.GetCoverUrl(CurrentTrack.Identifier)
                : null;

        var eb = track is not null
            ? new NowPlayingEmbed(track, Volume, Queue, coverUrl)
            : CurrentTrack is not null
                ? new NowPlayingEmbed(CurrentTrack, Volume, Queue, coverUrl)
                : new EmbedBuilder().WithTitle("No song is currently playing, will disconnect in 3 minutes.");

        var cb = track is not null
            ? new NowPlayingButtons(
                History.Count,
                State is PlayerState.Paused,
                LastVoteSkipInfo,
                Queue.Count,
                Volume,
                RepeatMode
            )
            : CurrentTrack is not null
                ? new NowPlayingButtons(
                    History.Count,
                    State is PlayerState.Paused,
                    LastVoteSkipInfo,
                    Queue.Count,
                    Volume,
                    RepeatMode
                )
                : new ComponentBuilder();

        if (NowPlayingMessage is null)
        {
            NowPlayingMessage = await TextChannel.SendMessageAsync(
                embed: eb.Build(),
                components: cb.Build()
            );
        }
        else
        {
            var check = await TextChannel.GetMessageAsync(NowPlayingMessage.Id);
            if (check is null)
            {
                NowPlayingMessage = await TextChannel.SendMessageAsync(
                    embed: eb.Build(),
                    components: cb.Build()
                );
            }
            else
            {
                await NowPlayingMessage.ModifyAsync(x =>
                {
                    x.Embed = eb.Build();
                    x.Components = cb.Build();
                });
            }
        }
    }

    protected override async ValueTask OnTrackStartedAsync(
        LavalinkTrack track,
        CancellationToken cancellationToken = new()
    )
    {
        await base.OnTrackStartedAsync(track, cancellationToken);
        await UpdateNowPlayingMessageAsync(track);
    }

    public Task DeleteMessageAsync()
    {
        return NowPlayingMessage is not null ? NowPlayingMessage.DeleteAsync() : Task.CompletedTask;
    }

    protected override ValueTask OnTrackEndedAsync(
        LavalinkTrack track,
        TrackEndReason endReason,
        CancellationToken cancellationToken = new()
    )
    {
        if (CurrentTrack is not null)
            History.Add(CurrentTrack);
        return base.OnTrackEndedAsync(track, endReason, cancellationToken);
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
                History,
                Volume,
                RepeatMode
            }
        );
    }
}
