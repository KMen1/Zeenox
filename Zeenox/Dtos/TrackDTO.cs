using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lavalink4NET.Integrations.LyricsJava;
using Lavalink4NET.Tracks;
using Zeenox.Models.Player;

namespace Zeenox.Dtos;

public class TrackDTO
{
    public TrackDTO(ExtendedTrackItem trackItem)
    {
        Id = trackItem.Track.Track.Identifier;
        Title = trackItem.Track.Title;
        Author = trackItem.Track.Author;
        Duration = trackItem.Track.Duration.TotalMilliseconds;
        RequestedBy = trackItem.RequestedBy is not null
            ? new SocketUserDTO(trackItem.RequestedBy)
            : null;
        Url = trackItem.Track.Uri?.ToString();
        ArtworkUrl = trackItem.ArtworkUri;
        TimedLyrics = trackItem.TimedLyrics;
        Lyrics = trackItem.Lyrics;
    }

    public TrackDTO(LavalinkTrack track)
    {
        Id = track.Identifier;
        Title = track.Title;
        Author = track.Author;
        Duration = track.Duration.TotalMilliseconds;
        Url = track.Uri?.ToString();
        ArtworkUrl = track.ArtworkUri?.ToString();
    }

    [JsonConstructor]
    public TrackDTO() { }

    public string Id { get; } = null!;
    public string Title { get; } = null!;
    public string Author { get; } = null!;
    public double Duration { get; }
    public string? Url { get; }
    public string? ArtworkUrl { get; }
    public SocketUserDTO? RequestedBy { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(TimedLyricsLineConverter))]
    public ImmutableArray<TimedLyricsLine>? TimedLyrics { get; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ImmutableArray<string>? Lyrics { get; }
}

public class TimedLyricsLineConverter : JsonConverter<ImmutableArray<TimedLyricsLine>>
{
    public override ImmutableArray<TimedLyricsLine> Read(ref Utf8JsonReader reader,
                                                         Type typeToConvert,
                                                         JsonSerializerOptions options)
    {
        reader.Read();
        var builder = ImmutableArray.CreateBuilder<TimedLyricsLine>();
        while (reader.TokenType != JsonTokenType.EndArray)
        {
            reader.Read();
            var line = reader.GetString();
            reader.Read();
            reader.Read();
            var start = TimeSpan.FromMilliseconds(reader.GetDouble());
            reader.Read();
            reader.Read();
            var end = TimeSpan.FromMilliseconds(reader.GetDouble());
            builder.Add(new TimedLyricsLine(line, new TimeRange(start, end)));
            reader.Read();
        }

        return builder.ToImmutable();
    }

    public override void Write(Utf8JsonWriter writer,
                               ImmutableArray<TimedLyricsLine> value,
                               JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var line in value)
        {
            writer.WriteStartObject();
            writer.WriteString("Line", line.Line);
            writer.WriteStartObject("Range");
            writer.WriteNumber("Start", line.Range.Start.TotalMilliseconds);
            writer.WriteNumber("End", line.Range.End.TotalMilliseconds);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }
}