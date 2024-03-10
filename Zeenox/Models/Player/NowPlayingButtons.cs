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
            "Previous",
            "previous",
            emote: new Emoji("⏮"),
            disabled: queue is { HasHistory: false, History.Count: 0 },
            row: 0
        );
        WithButton(
            isPaused ? "Resume" : "Pause",
            "pause",
            emote: isPaused ? new Emoji("▶") : new Emoji("⏸"),
            row: 0
        );
        WithButton("Stop", "stop", emote: new Emoji("⏹"), row: 0);
        WithButton(
            "Next",
            "skip",
            emote: new Emoji("⏭"),
            disabled: !autoPlay && queue.Count == 0,
            row: 0
        );
        WithButton("Down", "volumedown", emote: new Emoji("🔉"), disabled: volume == 0, row: 1);
        WithButton(
            "Loop "
            + loopMode switch
            {
                TrackRepeatMode.Track => "[Track]",
                TrackRepeatMode.Queue => "[Queue]",
                _ => "[Off]"
            },
            "loop",
            emote: new Emoji("🔁"),
            row: 1
        );
        WithButton(
            "Up",
            "volumeup",
            emote: new Emoji("🔊"),
            disabled: Math.Abs(volume - 0.5f) < 0.01f,
            row: 1
        );
        WithButton(
            "AutoPlay " + (autoPlay ? "[On]" : "[Off]"),
            "autoplay",
            emote: new Emoji("🔄"),
            row: 1
        );
    }
}