using Discord;
using Discord.WebSocket;
using Lavalink4NET.Players;
using Lavalink4NET.Tracks;
using Zeenox.Models.Player;
using Zeenox.Models.Socket;

namespace Zeenox.Models;

public class PlayerResumeSessionDto(string channelName, TrackDto currentTrack, int queueLength, List<TrackDto> queue, long timestamp)
{
    public string ChannelName { get; set; } = channelName;
    public TrackDto CurrentTrack { get; set; } = currentTrack;
    public int QueueLength { get; set; } = queueLength;
    public List<TrackDto> UpcomingFewTracks { get; set; } = queue;
    public long Timestamp { get; set; } = timestamp;

    public PlayerResumeSessionDto(PlayerResumeSession resumeSession, DiscordSocketClient client) :
        this(((IVoiceChannel)client.GetChannel(resumeSession.ChannelId)).Name,
            new TrackDto(new ExtendedTrackItem(
                new TrackReference(LavalinkTrack.Parse(resumeSession.CurrentTrack.Id, null)),
                client.GetUser(resumeSession.CurrentTrack.RequesterId))),
            resumeSession.Queue.Count,
            resumeSession.Queue.Take(5).Select(x =>
                new TrackDto(new ExtendedTrackItem(new TrackReference(LavalinkTrack.Parse(x.Id, null)),
                    client.GetUser(x.RequesterId)))).ToList(), resumeSession.Timestamp) {}

    public static PlayerResumeSessionDto? Create(PlayerResumeSession? resumeSession, DiscordSocketClient client)
    {
        return resumeSession is null ? null : new PlayerResumeSessionDto(resumeSession, client);
    }
}