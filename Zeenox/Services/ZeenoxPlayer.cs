using System.Globalization;
using Discord;
using Lavalink4NET.Player;
using Zeenox.Models;

namespace Zeenox.Services;

public class ZeenoxPlayer : VoteLavalinkPlayer
{
    public ZeenoxPlayer(ITextChannel textChannel, IVoiceChannel voiceChannel)
    {
        TextChannel = textChannel;
        VoiceChannel = voiceChannel;
    }

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
        Queue.AddRange(tracks[1..]);
        await UpdateNowPlayingMessageAsync();
    }

    public override async Task<int> PlayAsync(
        LavalinkTrack track,
        TimeSpan? startTime = null,
        TimeSpan? endTime = null,
        bool noReplace = false
    )
    {
        var result = await base.PlayAsync(track, startTime, endTime, noReplace);
        await UpdateNowPlayingMessageAsync();
        return result;
    }

    public override async Task<int> PlayAsync(
        LavalinkTrack track,
        bool enqueue,
        TimeSpan? startTime = null,
        TimeSpan? endTime = null,
        bool noReplace = false
    )
    {
        var result = await base.PlayAsync(track, enqueue, startTime, endTime, noReplace);
        await UpdateNowPlayingMessageAsync();
        return result;
    }

    public override async Task SkipAsync(int count = 1)
    {
        await base.SkipAsync(count);
        await UpdateNowPlayingMessageAsync();
    }

    public Task RewindAsync()
    {
        return PlayAsync(History[^1], false, TimeSpan.Zero, null, true);
    }

    public override async Task<UserVoteSkipInfo> VoteAsync(ulong userId, float percentage = 0.5f)
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

    public Task SetLoopModeAsync(PlayerLoopMode loopMode)
    {
        LoopMode = loopMode;
        return UpdateNowPlayingMessageAsync();
    }

    public Task ClearQueueAsync()
    {
        Queue.Clear();
        return UpdateNowPlayingMessageAsync();
    }

    public Task DistinctQueueAsync()
    {
        Queue.Distinct();
        return UpdateNowPlayingMessageAsync();
    }

    public override async Task SetVolumeAsync(
        float volume = 1,
        bool normalize = false,
        bool force = false
    )
    {
        await base.SetVolumeAsync(volume, normalize, force);
        await UpdateNowPlayingMessageAsync();
    }

    private async Task UpdateNowPlayingMessageAsync()
    {
        var context = (TrackContext)CurrentTrack?.Context!;

        EmbedBuilder eb;
        if (CurrentTrack is not null)
        {
            eb = new EmbedBuilder()
                .WithAuthor(
                    "Now Playing",
                    "https://bestanimations.com/media/discs/895872755cd-animated-gif-9.gif"
                )
                .WithTitle(
                    CurrentTrack.SourceName == "spotify"
                        ? $"{CurrentTrack.Author} - {CurrentTrack.Title}"
                        : CurrentTrack.Title
                )
                .WithUrl(CurrentTrack.Uri?.ToString() ?? "")
                //.WithImageUrl(context.CoverUrl)
                .WithColor(new Color(31, 31, 31))
                .AddField("Added By", context.Requester.Mention, true)
                .AddField("Length", $"`{CurrentTrack.Duration:mm\\:ss}`", true)
                .AddField(
                    "Volume",
                    $"`{Math.Round(Volume * 100).ToString(CultureInfo.InvariantCulture)}%`",
                    true
                );
            if (Queue.Count > 0)
                eb.AddField("Queue", $"`{Queue.Count.ToString()}`", true);
        }
        else
        {
            eb = new EmbedBuilder().WithTitle("No song is currently playing");
        }

        var cb = new ComponentBuilder();
        if (CurrentTrack is not null)
        {
            cb.WithButton(
                    "Back",
                    "previous",
                    emote: new Emoji("⏮"),
                    disabled: History.Count == 0,
                    row: 0
                )
                .WithButton(
                    State == PlayerState.Paused ? "Resume" : "Pause",
                    "pause",
                    emote: State == PlayerState.Paused ? new Emoji("▶") : new Emoji("⏸"),
                    row: 0
                )
                .WithButton("Stop", "stop", emote: new Emoji("⏹"), row: 0)
                .WithButton(
                    LastVoteSkipInfo is null
                        ? "Skip"
                        : $"Skip ({LastVoteSkipInfo.Votes.Count}/{Math.Floor(LastVoteSkipInfo.TotalUsers * LastVoteSkipInfo.Percentage)})",
                    "skip",
                    emote: new Emoji("⏭"),
                    disabled: Queue.Count == 0,
                    row: 0
                )
                .WithButton(
                    "Favorite",
                    "favorite",
                    emote: new Emoji("❤️"),
                    disabled: CurrentTrack is null,
                    row: 0
                )
                .WithButton(
                    "Down",
                    "volumedown",
                    emote: new Emoji("🔉"),
                    disabled: Volume == 0,
                    row: 1
                )
                /*.WithButton(LocalizedPlayer.Filter,
                    "filter",
                    emote: new Emoji("🎚"),
                    row: 1
                )*/
                .WithButton(
                    "Loop "
                        + LoopMode switch
                        {
                            PlayerLoopMode.Track => "[Track]",
                            PlayerLoopMode.Queue => "[Queue]",
                            _ => "[Off]"
                        },
                    "loop",
                    emote: new Emoji("🔁"),
                    row: 1
                )
                .WithButton(
                    "Up",
                    "volumeup",
                    emote: new Emoji("🔊"),
                    disabled: Math.Abs(Volume - 1.0f) < 0.01f,
                    row: 1
                );
        }

        if (NowPlayingMessage is null)
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
