using Discord;
using Discord.WebSocket;
using Lavalink4NET.Tracks;
using Zeenox.Models;
using Zeenox.Models.Player;

namespace Zeenox.Dtos;

public class ResumeSessionDTO(
    string channelName,
    TrackDTO currentTrack,
    int queueLength,
    List<TrackDTO> queue,
    long timestamp)
{
    public ResumeSessionDTO(ResumeSession resumeSession, DiscordSocketClient client) :
        this(((IVoiceChannel)client.GetChannel(resumeSession.ChannelId)).Name,
             new TrackDTO(new ExtendedTrackItem(
                              LavalinkTrack.Parse(resumeSession.CurrentTrack.Id, null),
                              client.GetUser(resumeSession.CurrentTrack.RequesterId.GetValueOrDefault()))),
             resumeSession.Queue.Count,
             resumeSession.Queue.Take(5).Select(x =>
                                                    new TrackDTO(new ExtendedTrackItem(LavalinkTrack.Parse(x.Id, null),
                                                                     client.GetUser(
                                                                         x.RequesterId.GetValueOrDefault())))).ToList(),
             resumeSession.Timestamp) { }

    public string ChannelName { get; set; } = channelName;
    public TrackDTO CurrentTrack { get; set; } = currentTrack;
    public int QueueLength { get; set; } = queueLength;
    public List<TrackDTO> UpcomingFewTracks { get; set; } = queue;
    public long Timestamp { get; set; } = timestamp;
}