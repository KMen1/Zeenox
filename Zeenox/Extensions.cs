using System.Text;
using Discord;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Tracks;

namespace Zeenox;

public static class Extensions
{
    public static string GetTitle(this LavalinkTrack track)
    {
        return track.SourceName == "spotify" ? $"{track.Author} - {track.Title}" : track.Title;
    }

    public static string GetTitle(this ITrackQueueItem queueItem)
    {
        var track = queueItem.Track!;
        return track.SourceName == "spotify" ? $"{track.Author} - {track.Title}" : track.Title;
    }

    public static string ToTimeString(this TimeSpan timeSpan)
    {
        return timeSpan.TotalHours < 1
            ? timeSpan.ToString(@"mm\:ss")
            : timeSpan.ToString(timeSpan.TotalDays < 1 ? @"hh\:mm\:ss" : @"dd\:hh\:mm\:ss");
    }

    public static Embed[] ToEmbeds(
        this IEnumerable<string> linesEnumerable,
        string title,
        int chunkSize
    )
    {
        var lines = linesEnumerable.ToList();
        var embeds = new List<Embed>();
        var sb = new StringBuilder();
        var firstEmbed = new EmbedBuilder().WithTitle(title);

        for (var i = 0; i < (chunkSize > lines.Count ? lines.Count : chunkSize); i++)
        {
            sb.AppendLine($"{i + 1}. {lines[i]}");
        }

        firstEmbed.WithDescription(sb.ToString());
        sb.Clear();
        embeds.Add(firstEmbed.Build());

        var lastIndex = chunkSize;
        var embedBs = lines
            .Skip(chunkSize)
            .Chunk(chunkSize)
            .Take(10)
            .Select(x =>
            {
                for (var i = 0; i < x.Length; i++)
                {
                    sb.AppendLine($"{lastIndex + i + 1}. {x[i]}");
                }

                var eb = new EmbedBuilder().WithDescription(sb.ToString()).Build();
                sb.Clear();
                lastIndex += x.Length;
                return eb;
            });

        embeds.AddRange(embedBs);
        return embeds.ToArray();
    }
}
