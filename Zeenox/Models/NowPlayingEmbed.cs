using System.Text;
using Discord;
using Lavalink4NET.Players.Queued;

namespace Zeenox.Models;

public class NowPlayingEmbed : EmbedBuilder
{
    public NowPlayingEmbed(ZeenoxTrackItem trackItem, float volume, ITrackQueue queue)
    {
        var track = trackItem.Reference.Track!;

        Author = new EmbedAuthorBuilder()
            .WithName("NOW PLAYING")
            .WithIconUrl("https://im2.ezgif.com/tmp/ezgif-2-81f6555576.gif");
        Title = track.GetTitle();
        Url = track.Uri?.ToString() ?? "";
        Color = new Color(31, 31, 31);
        ImageUrl = trackItem.GetThumbnailUrl();
        Footer = new EmbedFooterBuilder()
            .WithIconUrl(trackItem.RequestedBy.GetAvatarUrl())
            .WithText(
                $"Added by {trackItem.RequestedBy.Username} | Length: {track.Duration.ToTimeString()} | Volume: {Math.Round(volume * 200)}%"
            );
        if (queue.Count <= 0)
            return;
        var sb = new StringBuilder();
        var counter = 1;
        var tracks = queue.Take(5);
        foreach (var queueItem in tracks)
        {
            var nextTrack = queueItem.Track!;
            sb.AppendLine($"`{counter}. {nextTrack.GetTitle()}`");
            counter++;
        }

        if (queue.Count > 5)
            sb.AppendLine($"`and {queue.Count - 5} more...`");

        AddField("📃 Next Tracks", $"{sb.ToString().TrimEnd('\r', '\n')}");
    }
}
