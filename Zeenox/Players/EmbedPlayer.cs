using Discord;
using Discord.WebSocket;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Zeenox.Models.Player;

namespace Zeenox.Players;

public class EmbedPlayer
    (IPlayerProperties<MusicPlayer, EmbedPlayerOptions> properties) : MusicPlayer(properties)
{
    private ITextChannel? TextChannel { get; } = properties.Options.Value.TextChannel;
    public SocketVoiceChannel VoiceChannel { get; } = properties.Options.Value.VoiceChannel;
    private IUserMessage? NowPlayingMessage { get; set; }

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

    protected override async Task SetRepeatModeAsync(TrackRepeatMode repeatMode)
    {
        await base.SetRepeatModeAsync(repeatMode).ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    protected override async ValueTask<int> ClearQueueAsync()
    {
        var result = await base.ClearQueueAsync().ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
        return result;
    }

    protected override async ValueTask<int> DistinctQueueAsync()
    {
        var result = await base.DistinctQueueAsync().ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
        return result;
    }

    protected override async Task ReverseQueueAsync()
    {
        await base.ReverseQueueAsync().ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    protected override async ValueTask<bool> SkipToAsync(int index)
    {
        var result = await base.SkipToAsync(index).ConfigureAwait(false);
        if (result)
            await UpdateMessageAsync().ConfigureAwait(false);
        return result;
    }

    protected override async ValueTask<bool> RemoveAtAsync(int index)
    {
        var result = await base.RemoveAtAsync(index).ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
        return result;
    }

    protected override async Task ToggleAutoPlayAsync()
    {
        await base.ToggleAutoPlayAsync().ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    protected override async ValueTask ShuffleAsync()
    {
        await base.ShuffleAsync().ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public override async ValueTask<bool> MoveTrackAsync(int from, int to)
    {
        var result = await base.MoveTrackAsync(from, to).ConfigureAwait(false);
        if (result)
            await UpdateMessageAsync().ConfigureAwait(false);
        return result;
    }

    protected override async ValueTask SetVolumeAsync(
        int volume
    )
    {
        await base.SetVolumeAsync(volume).ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    public override async ValueTask StopAsync(CancellationToken cancellationToken = new())
    {
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    protected override async ValueTask NotifyTrackStartedAsync(
        ITrackQueueItem queueItem,
        CancellationToken cancellationToken = new()
    )
    {
        await base.NotifyTrackStartedAsync(queueItem, cancellationToken).ConfigureAwait(false);
        await UpdateMessageAsync(queueItem as ExtendedTrackItem).ConfigureAwait(false);
    }

    protected override async ValueTask NotifyTrackEnqueuedAsync(
        ITrackQueueItem queueItem,
        int position,
        CancellationToken cancellationToken = new()
    )
    {
        await base.NotifyTrackEnqueuedAsync(queueItem, position, cancellationToken).ConfigureAwait(false);
        await UpdateMessageAsync().ConfigureAwait(false);
    }

    private async Task UpdateMessageAsync(ExtendedTrackItem? track = null)
    {
        if (TextChannel is null)
            return;
        
        var actualTrack = track ?? CurrentItem;
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

    private EmbedBuilder GetEmbedBuilder(ExtendedTrackItem? track)
    {
        if (track is null)
        {
            return new EmbedBuilder().WithTitle(
                "There are no more tracks, I will disconnect in 3 minutes."
            );
        }

        return new NowPlayingEmbed(track, Volume, Queue);
    }

    private ComponentBuilder GetButtons(ExtendedTrackItem? track)
    {
        return track is null
            ? new ComponentBuilder().WithButton(
                "Disconnect Now",
                "disconnect",
                ButtonStyle.Danger,
                new Emoji("⚠️")
            )
            : new NowPlayingButtons(Queue, State is PlayerState.Paused, Volume, IsAutoPlayEnabled, RepeatMode);
    }

    public async Task DeleteNowPlayingMessageAsync()
    {
        if (NowPlayingMessage is null || TextChannel is null)
            return;
        
        if (await TextChannel.GetMessageAsync(NowPlayingMessage.Id).ConfigureAwait(false) is null)
            return;
        
        await NowPlayingMessage.DeleteAsync().ConfigureAwait(false);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await DeleteNowPlayingMessageAsync().ConfigureAwait(false);
        await base.DisposeAsyncCore().ConfigureAwait(false);
    }
}