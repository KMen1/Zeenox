using Discord;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Players.Vote;
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
    }

    private ITextChannel TextChannel { get; }
    private IVoiceChannel VoiceChannel { get; }
    private IUserMessage? NowPlayingMessage { get; set; }

    public async Task PlayAsync(IEnumerable<ZeenoxTrackItem> tracksEnumerable)
    {
        var tracks = tracksEnumerable.ToArray();
        if (tracks.Length == 0)
            return;

        await PlayAsync(tracks[0]).ConfigureAwait(false);
        await Queue.AddRangeAsync(tracks[1..]).ConfigureAwait(false);
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

    public async Task SkipToAsync(int index)
    {
        if (index < 0 || index >= Queue.Count)
            return;

        var track = Queue[index];
        await Queue.RemoveRangeAsync(0, index + 1).ConfigureAwait(false);
        await PlayAsync(track, false).ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public async Task RemoveAsync(int index)
    {
        if (index < 0 || index >= Queue.Count)
            return;

        await Queue.RemoveAtAsync(index).ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public async Task ShuffleAsync()
    {
        await Queue.ShuffleAsync().ConfigureAwait(false);
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

    protected override async ValueTask NotifyTrackStartedAsync(
        ITrackQueueItem queueItem,
        CancellationToken cancellationToken = new()
    )
    {
        await UpdateMessageAsync(queueItem as ZeenoxTrackItem).ConfigureAwait(false);
    }

    protected override async ValueTask NotifyTrackEnqueuedAsync(
        ITrackQueueItem queueItem,
        int position,
        CancellationToken cancellationToken = new()
    )
    {
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public override async ValueTask StopAsync(CancellationToken cancellationToken = new())
    {
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    private async Task UpdateMessageAsync(ZeenoxTrackItem? track = null)
    {
        var actualTrack = track ?? CurrentItem as ZeenoxTrackItem;
        var eb = GetEmbedBuilder(actualTrack);
        var cb = GetButtons(actualTrack);

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

    private EmbedBuilder GetEmbedBuilder(ZeenoxTrackItem? track)
    {
        if (track is null)
        {
            return new EmbedBuilder().WithTitle(
                "There are no more tracks, I will disconnect in 3 minutes."
            );
        }

        return new NowPlayingEmbed(track, Volume, Queue);
    }

    private ComponentBuilder GetButtons(ZeenoxTrackItem? track)
    {
        return track is null
            ? new ComponentBuilder()
            : new NowPlayingButtons(Queue, State is PlayerState.Paused, Volume, RepeatMode);
    }

    public Task DeleteNowPlayingMessageAsync()
    {
        return NowPlayingMessage?.DeleteAsync() ?? Task.CompletedTask;
    }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(
            new
            {
                TextChannelName = TextChannel.Name,
                VoiceChannelName = VoiceChannel.Name,
                State,
                Queue,
                Volume,
                RepeatMode
            }
        );
    }
}
