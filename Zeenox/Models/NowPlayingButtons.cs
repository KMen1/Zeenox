using Discord;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Players.Vote;

namespace Zeenox.Models;

public class NowPlayingButtons : ComponentBuilder
{
    public NowPlayingButtons(
        int historyCount,
        bool isPaused,
        UserVoteSkipInfo? voteSkipInfo,
        int queueCount,
        float volume,
        TrackRepeatMode loopMode
    )
    {
        WithButton("Back", "previous", emote: new Emoji("⏮"), disabled: historyCount == 0, row: 0);
        WithButton(
            isPaused ? "Resume" : "Pause",
            "pause",
            emote: isPaused ? new Emoji("▶") : new Emoji("⏸"),
            row: 0
        );
        WithButton("Stop", "stop", emote: new Emoji("⏹"), row: 0);
        WithButton(
            voteSkipInfo is null
                ? "Skip"
                : $"Skip ({voteSkipInfo.Votes.Length}/{Math.Floor(voteSkipInfo.TotalUsers * voteSkipInfo.Percentage)})",
            "skip",
            emote: new Emoji("⏭"),
            disabled: queueCount == 0,
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
            disabled: Math.Abs(volume - 1.0f) < 0.01f,
            row: 1
        );
        WithButton("Favorite", "favorite", emote: new Emoji("❤️"), row: 1);
    }
}
