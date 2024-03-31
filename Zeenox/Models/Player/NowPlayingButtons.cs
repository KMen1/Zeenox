using Discord;
using Lavalink4NET.Players.Queued;

namespace Zeenox.Models.Player;

public class NowPlayingButtons : ComponentBuilder
{
    public NowPlayingButtons(
        ITrackQueue queue,
        bool isPaused,
        float volume,
        bool autoPlay,
        TrackRepeatMode loopMode
    )
    {
        WithButton(
            customId: "previous",
            emote: Emote.Parse("<:playertrackprev:1223329575074005063>"),
            disabled: queue is { HasHistory: false, History.Count: 0 },
            row: 0
        );
        WithButton(
            customId: "pause",
            emote: isPaused
                ? Emote.Parse("<:playerplay:1223329542979190884>")
                : Emote.Parse("<:playerpause:1223329563707314267>"),
            row: 0
        );
        WithButton(
            customId: "stop",
            emote: Emote.Parse("<:playerstop:1223330575457189948>"),
            row: 0
        );
        WithButton(
            customId: "skip",
            emote: Emote.Parse("<:playertracknext:1223329623467626688>"),
            disabled: !autoPlay && queue.Count == 0,
            row: 0
        );
        WithButton(
            customId: "volumedown",
            emote: Emote.Parse("<:volumedown:1223331122373722262>"),
            disabled: volume == 0,
            row: 1
        );
        WithButton(
            customId: "loop",
            emote: loopMode switch
            {
                TrackRepeatMode.None => Emote.Parse("<:repeatoff:1223331694959136929>"),
                TrackRepeatMode.Track => Emote.Parse("<:repeatonce:1223331684519383181>"),
                TrackRepeatMode.Queue => Emote.Parse("<:repeat:1223331661916278875>"),
                _ => Emote.Parse("<:repeatoff:1223331694959136929>")
            },
            row: 1
        );
        WithButton(
            customId: "autoplay",
            emote: Emote.Parse("<:autoplay:1223331067797311709>"),
            row: 1
        );
        WithButton(
            customId: "volumeup",
            emote: Emote.Parse("<:volumeup:1223331102052323431>"),
            disabled: Math.Abs(volume - 0.5f) < 0.01f,
            row: 1
        );
    }
}
