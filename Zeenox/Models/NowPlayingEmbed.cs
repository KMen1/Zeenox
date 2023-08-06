using System.Text;
using Discord;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Tracks;

namespace Zeenox.Models;

public class NowPlayingEmbed : EmbedBuilder
{
    public NowPlayingEmbed(
        LavalinkTrack track,
        float volume,
        ITrackQueue queue,
        string? coverUrl = ""
    )
    {
        //var context = (TrackContext)track.Context!;
        Author = new EmbedAuthorBuilder()
            .WithName("NOW PLAYING")
            .WithIconUrl("https://bestanimations.com/media/discs/895872755cd-animated-gif-9.gif");
        Title = track.SourceName == "spotify" ? $"{track.Author} - {track.Title}" : track.Title;
        Url = track.Uri?.ToString() ?? "";
        Color = new Color(31, 31, 31);
        ImageUrl = coverUrl;
        //AddField("Added By", context.Requester.Mention, true);
        AddField("🕐 Length", $"`{track.Duration:mm\\:ss}`", true);
        AddField("🔊 Volume", $"`{Math.Round(volume * 200)}%`", true);
        if (queue.Count > 0)
        {
            var sb = new StringBuilder();
            var counter = 1;
            var tracks = queue.Take(5);
            foreach (var queuedTrack in tracks)
            {
                var qTrack = queuedTrack.Track.Track!;
                var nextTitle =
                    qTrack.SourceName == "spotify"
                        ? $"{qTrack.Author} - {qTrack.Title}"
                        : qTrack.Title;
                sb.AppendLine($"`{counter}. {nextTitle} ({qTrack.Duration:mm\\:ss})`");
                counter++;
            }

            if (queue.Count > 5)
                sb.AppendLine($"`and {queue.Count - 5} more...`");

            AddField("📃 Queue", $"`{queue.Count}`", true);
            AddField(
                "⏭ Next Songs",
                $"{sb.ToString().TrimEnd('\r', '\n')}",
                true
            );
        }
    }
}
