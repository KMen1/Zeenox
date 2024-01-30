using System.Text;
using Discord;
using Lavalink4NET.Players.Queued;

namespace Zeenox.Models.Player;

public class NowPlayingEmbed : EmbedBuilder
{
    public NowPlayingEmbed(ExtendedTrackItem trackItem, float volume, ITrackQueue queue)
    {
        var track = trackItem.Track;

        Author = new EmbedAuthorBuilder()
            .WithName("NOW PLAYING")
            .WithIconUrl("https://im2.ezgif.com/tmp/ezgif-2-81f6555576.gif");
        Title = track.Title;
        Url = track.Uri?.ToString() ?? "";
        Color = new Color(31, 31, 31);
        ImageUrl = trackItem.AlbumImageUrl;
        Footer = new EmbedFooterBuilder()
            .WithIconUrl(trackItem.RequestedBy?.GetAvatarUrl())
            .WithText(
                $"Added by {trackItem.RequestedBy?.Username} | Length: {track.Duration.ToTimeString()} | Volume: {Math.Round(volume * 200)}%"
            );
        if (queue.Count <= 0)
            return;
        var sb = new StringBuilder();
        var counter = 1;
        var tracks = queue.Take(5);
        foreach (var queueItem in tracks)
        {
            var nextTrack = (ExtendedTrackItem)queueItem;
            sb.AppendLine($"`{counter}. {nextTrack.Title}`");
            counter++;
        }

        if (queue.Count > 5)
            sb.AppendLine($"`and {queue.Count - 5} more...`");

        AddField("📃 Upcoming", $"{sb.ToString().TrimEnd('\r', '\n')}");
    }
}
